using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NCrontab.Scheduler.Extensions;
using NCrontab.Scheduler.Internals;

namespace NCrontab.Scheduler
{
    /// <inheritdoc/>
    public class Scheduler : IScheduler
    {
        private static readonly Lazy<IScheduler> Implementation =
            new Lazy<IScheduler>(CreateScheduler, LazyThreadSafetyMode.PublicationOnly);

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
            : this(logger, new SystemDateTime())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="dateTime">The datetime provider.</param>
        internal Scheduler(
            ILogger<Scheduler> logger,
            IDateTime dateTime)
        {
            this.dateTime = dateTime;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public void AddTask(IScheduledTask scheduledTask)
        {
            this.logger.LogDebug(
                $"AddTask: taskId={scheduledTask.Id:B}, crontabSchedule={scheduledTask.CrontabSchedule}");

            this.AddTaskInternal(scheduledTask);
        }

        /// <inheritdoc/>
        public void AddTask(IAsyncScheduledTask scheduledTask)
        {
            this.logger.LogDebug(
                $"AddTask: taskId={scheduledTask.Id:B}, crontabSchedule={scheduledTask.CrontabSchedule}");

            this.AddTaskInternal(scheduledTask);
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
            this.logger.LogDebug($"UpdateTask: taskId={taskId:B}, crontabSchedule={crontabSchedule}");

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

                    var now = this.dateTime.UtcNow;
                    var (nextOccurrence, taskIds) = this.GetScheduledTasksToRunAndHowLongToWait(now);

                    TimeSpan timeToWait;
                    if (taskIds.Count == 0)
                    {
                        timeToWait = TaskHelper.InfiniteTimeSpan;
                        this.logger.LogInformation(
                            $"Scheduler is waiting for tasks. " +
                            $"Use {nameof(IScheduler.AddTask)} methods to add tasks.");
                    }
                    else
                    {
                        timeToWait = nextOccurrence.Subtract(now).RoundUp(MaxDelayRounding);

                        this.logger.LogInformation(
                            $"Scheduling next event:{Environment.NewLine}" +
                            $" --> nextOccurrence: {nextOccurrence:O}{Environment.NewLine}" +
                            $" --> timeToWait: {timeToWait}{Environment.NewLine}" +
                            $" --> taskIds ({taskIds.Count}): {string.Join(", ", taskIds.Select(id => $"{id:B}"))}");
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

                    ITask[] scheduledTasksToRun;
                    lock (this.threadLock)
                    {
                        scheduledTasksToRun = this.scheduledTasks.Where(m => taskIds.Contains(m.Id)).ToArray();
                    }

                    if (scheduledTasksToRun.Length > 0)
                    {
                        var signalTime = this.dateTime.UtcNow;
                        var timingInaccuracy = signalTime - nextOccurrence;
                        this.logger.LogInformation(
                            $"Starting scheduled event:{Environment.NewLine}" +
                            $" --> signalTime: {signalTime:O} (deviation: {timingInaccuracy.TotalMilliseconds}ms){Environment.NewLine}" +
                            $" --> scheduledTasksToRun ({scheduledTasksToRun.Length}): {string.Join(", ", scheduledTasksToRun.Select(t => $"{t.Id:B}"))}");

                        this.RaiseNextEvent(signalTime, scheduledTasksToRun);

                        foreach (var task in scheduledTasksToRun)
                        {
                            if (this.localCancellationTokenSource.IsCancellationRequested)
                            {
                                this.logger.LogDebug("Cancellation requested");
                                break;
                            }

                            this.logger.LogDebug($"Starting task with Id={task.Id:B}...");

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
                                this.logger.LogError(e, $"Task with Id={task.Id:B} failed with exception");
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

        private (DateTime, IReadOnlyCollection<Guid>) GetScheduledTasksToRunAndHowLongToWait(DateTime now)
        {
            var lowestNextTimeToRun = DateTime.MaxValue;
            var lowestIds = new List<Guid>();

            lock (this.threadLock)
            {
                foreach (var scheduledTask in this.scheduledTasks)
                {
                    var nextTimeToRun = scheduledTask.CrontabSchedule.GetNextOccurrence(now);
                    if (nextTimeToRun == default)
                    {
                        continue;
                    }

                    if (nextTimeToRun < lowestNextTimeToRun)
                    {
                        lowestIds.Clear();
                        lowestIds.Add(scheduledTask.Id);
                        lowestNextTimeToRun = nextTimeToRun;
                    }
                    else if (nextTimeToRun == lowestNextTimeToRun)
                    {
                        lowestIds.Add(scheduledTask.Id);
                    }
                }
            }

            return (lowestNextTimeToRun, lowestIds);
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
                this.externalCancellationToken.Register(() => this.localCancellationTokenSource.Cancel());
            }
        }

        public event EventHandler<ScheduledEventArgs> Next;

        private void RaiseNextEvent(DateTime signalTime, params ITask[] tasks)
        {
            try
            {
                this.Next?.Invoke(this, new ScheduledEventArgs(signalTime, tasks.Select(t => t.Id).ToArray()));
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