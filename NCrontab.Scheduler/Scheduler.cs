using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NCrontab.Scheduler.Extensions;
using NCrontab.Scheduler.Internals;

namespace NCrontab.Scheduler
{
    /// <inheritdoc/>
    public class Scheduler : IScheduler
    {
        private static readonly Lazy<IScheduler> Implementation = new Lazy<IScheduler>(CreateScheduler, LazyThreadSafetyMode.PublicationOnly);

        public static IScheduler Current => Implementation.Value;

        private static IScheduler CreateScheduler()
        {
            return new Scheduler();
        }

        private static readonly TimeSpan MaxDelayRounding = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan DurationWarningThreshold = TimeSpan.FromMinutes(1);
        private readonly object threadLock = new object();

        private readonly List<ITask> scheduledTasks = new List<ITask>();
        private readonly IDateTime dateTime;
        private readonly ISchedulerOptions schedulerOptions;
        private readonly ILogger<Scheduler> logger;
        private CancellationTokenSource localCancellationTokenSource;
        private CancellationToken externalCancellationToken;
        private bool isRunning;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        public Scheduler()
            : this(new NullLogger<Scheduler>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public Scheduler(ILogger<Scheduler> logger)
            : this(logger, new SchedulerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="schedulerOptions">The scheduler options.</param>
        public Scheduler(ILogger<Scheduler> logger, IOptions<SchedulerOptions> schedulerOptions)
            : this(logger, schedulerOptions.Value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="schedulerOptions">The scheduler options.</param>
        public Scheduler(ILogger<Scheduler> logger, ISchedulerOptions schedulerOptions)
            : this(logger, new SystemDateTime(), schedulerOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="dateTime">The datetime provider.</param>
        /// <param name="schedulerOptions">The scheduler options.</param>
        internal Scheduler(
            ILogger<Scheduler> logger,
            IDateTime dateTime,
            ISchedulerOptions schedulerOptions)
        {
            this.logger = logger;
            this.dateTime = dateTime;
            this.schedulerOptions = schedulerOptions;
        }

        /// <inheritdoc/>
        public void AddTask(IScheduledTask task)
        {
            this.logger.LogDebug(
                $"AddTask: task={FormatTask(task, this.schedulerOptions.Logging)}, crontabSchedule={task.CrontabSchedule}");

            this.AddTaskInternal(task);
        }

        /// <inheritdoc/>
        public void AddTask(IAsyncScheduledTask task)
        {
            this.logger.LogDebug(
                $"AddTask: task={FormatTask(task, this.schedulerOptions.Logging)}, crontabSchedule={task.CrontabSchedule}");

            this.AddTaskInternal(task);
        }

        private void AddTaskInternal(ITask scheduledTask)
        {
            lock (this.threadLock)
            {
                this.scheduledTasks.Add(scheduledTask);

                if (this.IsRunning)
                {
                    this.ResetScheduler();
                }
            }
        }

        /// <inheritdoc/>
        public ITask GetTaskById(Guid taskId)
        {
            lock (this.threadLock)
            {
                return this.scheduledTasks.SingleOrDefault(t => t.Id == taskId);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<ITask> GetTasks()
        {
            lock (this.threadLock)
            {
                return this.scheduledTasks.ToList();
            }
        }

        /// <inheritdoc/>
        public void UpdateTask(ITask scheduledTask)
        {
            this.UpdateTask(scheduledTask.Id, scheduledTask.CrontabSchedule);
        }

        /// <inheritdoc/>
        public void UpdateTask(Guid taskId, CrontabSchedule crontabSchedule)
        {
            this.logger.LogDebug($"UpdateTask: taskId={taskId.ToString(this.schedulerOptions.Logging.TaskIdFormatter)}, crontabSchedule={crontabSchedule}");

            lock (this.threadLock)
            {
                var existingScheduledTask = this.GetTaskById(taskId);
                if (existingScheduledTask == null)
                {
                    throw new InvalidOperationException($"UpdateTask: task with Id={taskId} could not be found.");
                }
                else
                {
                    existingScheduledTask.CrontabSchedule = crontabSchedule;
                }

                if (this.IsRunning)
                {
                    this.ResetScheduler();
                }
            }
        }

        /// <inheritdoc/>
        public (Guid TaskId, bool Removed)[] RemoveTasks(params ITask[] tasks)
        {
            lock (this.threadLock)
            {
                var results = new List<(Guid TaskId, bool Removed)>();

                foreach (var task in tasks)
                {
                    var removed = this.scheduledTasks.Remove(task);
                    results.Add((task.Id, removed));
                }

                var removedCount = results.Count(r => r.Removed);
                var totalCount = tasks.Length;

                this.logger.LogDebug(
                    $"RemoveTasks: {removedCount}{(removedCount != totalCount ? $"/{totalCount}" : "")}{Environment.NewLine}" +
                    $"{string.Join(Environment.NewLine, results.Select(r => $"> task.Id={r.TaskId:B} -> {(r.Removed ? "removed" : "not found")}"))}");

                if (this.IsRunning)
                {
                    this.ResetScheduler();
                }

                return results.ToArray();
            }
        }

        /// <inheritdoc/>
        public void RemoveAllTasks()
        {
            lock (this.threadLock)
            {
                this.logger.LogDebug($"RemoveAllTasks: Count={this.scheduledTasks.Count}");

                this.scheduledTasks.Clear();

                if (this.IsRunning)
                {
                    this.ResetScheduler();
                }
            }
        }

        /*
         * This method depends on the system clock.
         * This means that the time delay will approximately equal the resolution of the system clock
         * if the millisecondsDelay argument is less than the resolution of the system clock,
         * which is approximately 15 milliseconds on Windows systems.
         */
        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (this.threadLock)
            {
                if (this.IsRunning)
                {
                    throw new InvalidOperationException("Scheduler is already running");
                }

                this.externalCancellationToken = cancellationToken;
                this.RegisterLocalCancellationToken();

                this.IsRunning = true;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!this.IsRunning)
                    {
                        return;
                    }

                    var loggingOptions = this.schedulerOptions.Logging;

                    var now = this.GetCurrentDate(this.schedulerOptions.DateTimeKind);
                    var utcNow = now.ToUniversalTime();
                    var (startDateUtc, nextTasks) = this.GetScheduledTasksToRunAndHowLongToWait(now);

                    TimeSpan timeToWait;
                    if (nextTasks.Count == 0)
                    {
                        timeToWait = TaskHelper.InfiniteTimeSpan;
                        this.logger.LogInformation(
                            $"Scheduler is waiting for tasks. " +
                            $"Use {nameof(IScheduler.AddTask)} methods to add tasks.");
                    }
                    else
                    {
                        timeToWait = startDateUtc.Subtract(utcNow).RoundUp(MaxDelayRounding);

                        var displayStartDate = this.schedulerOptions.Logging.DateTimeKind == DateTimeKind.Utc
                            ? startDateUtc
                            : startDateUtc.ToLocalTime();

                        this.logger.LogInformation(
                            $"Scheduling next event:{Environment.NewLine}" +
                            $" --> nextOccurrence: {displayStartDate:O}{Environment.NewLine}" +
                            $" --> timeToWait: {timeToWait}{Environment.NewLine}" +
                            $" --> nextTasks ({nextTasks.Count}): {string.Join(", ", nextTasks.Select(t => FormatTask(t, loggingOptions)))}");
                    }

                    var isCancellationRequested = await TaskHelper.LongDelay(this.dateTime, timeToWait, this.localCancellationTokenSource.Token)
                        .ContinueWith(_ =>
                        {
                            var userCanceled = cancellationToken.IsCancellationRequested;
                            return userCanceled;
                        }, CancellationToken.None);

                    if (!this.IsRunning)
                    {
                        return;
                    }

                    if (isCancellationRequested)
                    {
                        this.logger.LogDebug("Cancellation requested");
                        return;
                    }

                    // Re-evaluate list of tasks to run. This is necessary since
                    // the original list of scheduled tasks may be changed in the meantime.
                    lock (this.threadLock)
                    {
                        var newNextTasks = this.scheduledTasks.Where(m => nextTasks.Select(t => t.Id).Contains(m.Id)).ToArray();
                        //if (newNextTasks.Length != nextTasks.Count)
                        //{
                        //    Debugger.Break();
                        //}

                        nextTasks = newNextTasks;
                    }

                    if (nextTasks.Count > 0)
                    {
                        var signalTime = this.dateTime.UtcNow;
                        var timingInaccuracy = signalTime - startDateUtc;

                        this.logger.LogInformation(
                            $"Starting scheduled event:{Environment.NewLine}" +
                            $" --> signalTime: {signalTime:O} (deviation: {timingInaccuracy.TotalMilliseconds}ms){Environment.NewLine}" +
                            $" --> nextTasks ({nextTasks.Count}): {string.Join(", ", nextTasks.Select(t => FormatTask(t, loggingOptions)))}");

                        var nextTaskIds = nextTasks.Select(t => t.Id).ToArray();
                        this.RaiseNextEvent(signalTime, nextTaskIds);

                        foreach (var task in nextTasks)
                        {
                            if (this.localCancellationTokenSource.IsCancellationRequested)
                            {
                                this.logger.LogDebug("Cancellation requested");
                                break;
                            }

                            this.logger.LogDebug($"Starting task {FormatTask(task, loggingOptions)}...");

                            try
                            {
                                if (task is IScheduledTask scheduledTask)
                                {
                                    scheduledTask.Run(this.localCancellationTokenSource.Token);
                                }

                                if (task is IAsyncScheduledTask asyncScheduledTask)
                                {
                                    await asyncScheduledTask.RunAsync(this.localCancellationTokenSource.Token);
                                }
                            }
                            catch (Exception e)
                            {
                                this.logger.LogError(e, $"Task {FormatTask(task, loggingOptions)} failed with exception");
                            }
                        }

                        var endTime = this.dateTime.UtcNow;
                        var duration = endTime - signalTime;
                        this.logger.Log(
                            duration >= DurationWarningThreshold ? LogLevel.Warning : LogLevel.Debug,
                            $"Execution finished after {duration}");
                    }
                }
            }
            finally
            {
                this.IsRunning = false;
            }
        }

        private static string FormatTask(ITask t, LoggingOptions loggingOptions)
        {
            switch (loggingOptions.LogIdentifier)
            {
                case LogIdentifier.TaskName:
                    return !string.IsNullOrEmpty(t.Name)
                        ? t.Name
                        : t.Id.ToString(loggingOptions.TaskIdFormatter);

                case LogIdentifier.TaskIdAndName:
                    return !string.IsNullOrEmpty(t.Name)
                        ? $"{t.Id.ToString(loggingOptions.TaskIdFormatter)} {t.Name}"
                        : t.Id.ToString(loggingOptions.TaskIdFormatter);

                case LogIdentifier.TaskNameAndId:
                    return !string.IsNullOrEmpty(t.Name)
                        ? $"{t.Name} {t.Id.ToString(loggingOptions.TaskIdFormatter)}"
                        : t.Id.ToString(loggingOptions.TaskIdFormatter);

                case LogIdentifier.TaskId:
                default:
                    return t.Id.ToString(loggingOptions.TaskIdFormatter);
            }
        }

        private DateTime GetCurrentDate(DateTimeKind dateTimeKind)
        {
            return dateTimeKind == DateTimeKind.Local
                ? this.dateTime.Now
                : this.dateTime.UtcNow;
        }

        public bool IsRunning
        {
            get => this.isRunning;
            private set
            {
                if (this.isRunning != value)
                {
                    this.isRunning = value;
                    this.logger.LogDebug(value ? "Started" : "Stopped");
                }
            }
        }

        private (DateTime StartDateUtc, IReadOnlyCollection<ITask> Tasks) GetScheduledTasksToRunAndHowLongToWait(DateTime now)
        {
            var lowestNextTimeToRun = DateTime.MaxValue;
            var lowestTasks = new List<ITask>();

            lock (this.threadLock)
            {
                foreach (var scheduledTask in this.scheduledTasks)
                {
                    var nextTimeToRun = scheduledTask.CrontabSchedule.GetNextOccurrence(now).ToUniversalTime();
                    if (nextTimeToRun == default)
                    {
                        continue;
                    }

                    if (nextTimeToRun < lowestNextTimeToRun)
                    {
                        lowestTasks.Clear();
                        lowestTasks.Add(scheduledTask);
                        lowestNextTimeToRun = nextTimeToRun;
                    }
                    else if (nextTimeToRun == lowestNextTimeToRun)
                    {
                        lowestTasks.Add(scheduledTask);
                    }
                }
            }

            return (lowestNextTimeToRun, lowestTasks);
        }

        private void ResetScheduler()
        {
            lock (this.threadLock)
            {
                this.localCancellationTokenSource.Cancel();

                this.RegisterLocalCancellationToken();
            }
        }

        private void RegisterLocalCancellationToken()
        {
            lock (this.threadLock)
            {
                this.localCancellationTokenSource = new CancellationTokenSource();
                this.externalCancellationToken.Register(this.localCancellationTokenSource.Cancel);
            }
        }

        public event EventHandler<ScheduledEventArgs> Next;

        private void RaiseNextEvent(DateTime signalTime, params Guid[] taskIds)
        {
            try
            {
                this.Next?.Invoke(this, new ScheduledEventArgs(signalTime, taskIds));
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "RaiseNextEvent failed with exception");
            }
        }

        public void Stop()
        {
            this.logger.LogInformation("Stopping...");

            lock (this.threadLock)
            {
                if (this.IsRunning)
                {
                    this.localCancellationTokenSource.Cancel();
                    this.IsRunning = false;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    lock (this.threadLock)
                    {
                        this.scheduledTasks.Clear();
                        if (this.IsRunning)
                        {
                            this.Stop();
                        }
                    }
                }

                this.disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}