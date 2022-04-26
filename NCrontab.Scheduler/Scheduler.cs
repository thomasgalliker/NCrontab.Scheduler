using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

        private static readonly TimeSpan DurationWarningThresold = new TimeSpan(0, 1, 0);
        private readonly object threadLock = new object();

        private readonly List<ITask> scheduledTasks = new List<ITask>();
        private readonly IDateTime dateTime;
        private readonly ILogger<Scheduler> logger;
        private CancellationTokenSource localCancellationTokenSource;
        private CancellationToken externalCancellationToken;
        private bool isRunning;
        private bool disposed;

        public Scheduler()
            : this(new NullLogger<Scheduler>(), new SystemDateTime())
        {
        }

        /// <inheritdoc/>
        public Scheduler(ILogger<Scheduler> logger)
            : this(logger, new SystemDateTime())
        {
        }

        /// <inheritdoc/>
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
            this.logger.LogDebug($"AddTask: taskId={scheduledTask.Id:B}, crontabSchedule={scheduledTask.CrontabSchedule}");

            this.AddTaskInternal(scheduledTask);
        }

        /// <inheritdoc/>
        public void AddTask(IAsyncScheduledTask scheduledTask)
        {
            this.logger.LogDebug($"AddTask: taskId={scheduledTask.Id:B}, crontabSchedule={scheduledTask.CrontabSchedule}");

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
        public bool RemoveTask(Guid taskId)
        {
            this.logger.LogDebug($"RemoveTask: taskId={taskId:B}");

            lock (this.threadLock)
            {
                var existingScheduledTask = this.GetTaskById(taskId);
                if (existingScheduledTask != null)
                {
                    var removed = this.scheduledTasks.Remove(existingScheduledTask);

                    if (this.IsRunning)
                    {
                        this.ResetScheduler();
                    }

                    return removed;
                }

                this.logger.LogWarning($"RemoveTask: Task with taskId={taskId:B} could not be found");

                return false;
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

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (this.threadLock)
            {
                if (this.IsRunning)
                {
                    throw new InvalidOperationException($"Scheduler is already running");
                }

                this.externalCancellationToken = cancellationToken;
                this.RegisterLocalCancelationToken();

                this.IsRunning = true;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var now = this.dateTime.Now;
                    var (nextOccurrence, taskIds) = this.GetScheduledTasksToRunAndHowLongToWait(now);

                    TimeSpan timeToWait;

                    if (taskIds.Count == 0)
                    {
                        timeToWait = TimeSpan.MaxValue;
                    }
                    else
                    {
                        timeToWait = nextOccurrence.Subtract(now);
                    }

                    var millisecondsDelay = (int)Math.Min(timeToWait.TotalMilliseconds, int.MaxValue);
                    if (millisecondsDelay == int.MaxValue)
                    {
                        this.logger.LogInformation(
                            $"Scheduler is waiting for tasks. " +
                            $"Use {nameof(IScheduler.AddTask)} methods to add tasks.");
                    }
                    else
                    {
                        this.logger.LogInformation(
                            $"Scheduling next event:{Environment.NewLine}" +
                            $" --> nextOccurrence: {nextOccurrence:O}{Environment.NewLine}" +
                            $" --> millisecondsDelay: {millisecondsDelay} ({timeToWait}){Environment.NewLine}" +
                            $" --> taskIds ({taskIds.Count}): {string.Join(", ", taskIds.Select(id => $"{id:B}"))}");
                    }

                    var isCancellationRequested = await Task.Delay(millisecondsDelay, this.localCancellationTokenSource.Token)
                        .ContinueWith(ct =>
                        {
                            var userCanceled = cancellationToken.IsCancellationRequested;
                            var tokenCanceled = ct.Status == TaskStatus.Canceled;
                            return userCanceled /*|| tokenCanceled*/;
                        });

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
                        var startTime = this.dateTime.Now;
                        this.logger.LogDebug($"Starting scheduled event{Environment.NewLine}" +
                            $" --> startTime: {startTime:O}{Environment.NewLine}" +
                            $" --> scheduledTasksToRun ({scheduledTasksToRun.Length}): {string.Join(", ", scheduledTasksToRun.Select(t => $"{t.Id:B}"))}");

                        this.RaiseNextEvent(startTime, scheduledTasksToRun);

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

                        var endTime = this.dateTime.Now;
                        var duration = endTime - startTime;
                        this.logger.Log(
                            duration > DurationWarningThresold ? LogLevel.Warning : LogLevel.Debug,
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

                    if (value)
                    {
                        this.logger.LogDebug("Started");
                    }
                    else
                    {
                        this.logger.LogDebug("Stopped");
                    }
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

                this.RegisterLocalCancelationToken();
            }
        }

        private void RegisterLocalCancelationToken()
        {
            lock (this.threadLock)
            {
                this.localCancellationTokenSource = new CancellationTokenSource();
                this.externalCancellationToken.Register(() => this.localCancellationTokenSource.Cancel());
            }
        }

        public event EventHandler<ScheduledEventArgs> Next;

        private void RaiseNextEvent(DateTime signalTime, params ITask[] scheduledTasks)
        {
            try
            {
                Next?.Invoke(this, new ScheduledEventArgs(signalTime, scheduledTasks.Select(t => t.Id).ToArray()));
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
                if (!this.IsRunning)
                {
                    throw new InvalidOperationException($"Scheduler is not running");
                }

                this.localCancellationTokenSource.Cancel();
                this.IsRunning = false;
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
