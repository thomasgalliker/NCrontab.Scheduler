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
        private static readonly TimeSpan DurationWarningThresold = new TimeSpan(0, 1, 0);
        private readonly object threadLock = new object();

        private readonly List<ScheduledTaskInternal> scheduledTasks = new List<ScheduledTaskInternal>();
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
        public void AddTask(Guid id, string cronExpression, Func<CancellationToken, Task> func)
        {
            this.AddTask(id, CrontabSchedule.Parse(cronExpression), func);
        }

        /// <inheritdoc/>
        public void AddTask(Guid id, CrontabSchedule cronExpression, Func<CancellationToken, Task> func)
        {
            this.logger.LogDebug($"AddTask: id={id:B}, cronExpression={cronExpression}");

            var scheduledTask = new ScheduledTaskInternal(id, cronExpression, func);

            this.AddTaskInternal(scheduledTask);
        }

        /// <inheritdoc/>
        public Guid AddTask(string cronExpression, Func<CancellationToken, Task> func)
        {
            return this.AddTask(CrontabSchedule.Parse(cronExpression), func);
        }

        /// <inheritdoc/>
        public Guid AddTask(CrontabSchedule cronExpression, Func<CancellationToken, Task> func)
        {
            var id = Guid.NewGuid();

            this.AddTask(id, cronExpression, func);

            return id;
        }

        /// <inheritdoc/>
        public void AddTask(Guid id, string cronExpression, Action<CancellationToken> action)
        {
            this.AddTask(id, CrontabSchedule.Parse(cronExpression), action);
        }

        /// <inheritdoc/>
        public void AddTask(Guid id, CrontabSchedule cronExpression, Action<CancellationToken> action)
        {
            this.logger.LogDebug($"AddTask: id={id:B}, cronExpression={cronExpression}");

            var scheduledTask = new ScheduledTaskInternal(id, cronExpression, action);

            this.AddTaskInternal(scheduledTask);
        }

        private void AddTaskInternal(ScheduledTaskInternal scheduledTask)
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
        public Guid AddTask(string cronExpression, Action<CancellationToken> action)
        {
            return this.AddTask(CrontabSchedule.Parse(cronExpression), action);
        }

        /// <inheritdoc/>
        public Guid AddTask(CrontabSchedule cronExpression, Action<CancellationToken> action)
        {
            var id = Guid.NewGuid();

            this.AddTask(id, cronExpression, action);

            return id;
        }

        /// <inheritdoc/>
        public void RemoveTask(Guid taskId)
        {
            this.logger.LogDebug($"RemoveTask: taskId={taskId:B}");

            lock (this.threadLock)
            {
                var scheduledTask = this.scheduledTasks.SingleOrDefault(t => t.Id == taskId);
                if (scheduledTask != null)
                {
                    this.scheduledTasks.Remove(scheduledTask);

                    if (this.IsRunning)
                    {
                        this.ResetScheduler();
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Start(CancellationToken cancellationToken = default)
        {
            Task.Run(() => this.StartAsync(cancellationToken));
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
                            $"Use {nameof(this.AddTask)} methods to add tasks.");
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

                    ScheduledTaskInternal[] scheduledTasksToRun;
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

                        foreach (var scheduledTask in scheduledTasksToRun)
                        {
                            this.logger.LogDebug($"Starting task with Id={scheduledTask.Id:B}...");
                            if (this.localCancellationTokenSource.IsCancellationRequested)
                            {
                                this.logger.LogDebug("Cancellation requested");
                                break;
                            }

                            try
                            {
                                if (scheduledTask.Action is object)
                                {
                                    scheduledTask.Action.Invoke(this.localCancellationTokenSource.Token);
                                }

                                if (scheduledTask.ActionTask is object)
                                {
                                    await scheduledTask.ActionTask.Invoke(this.localCancellationTokenSource.Token);
                                }
                            }
                            catch (Exception e)
                            {
                                this.logger.LogError(e, $"Task with Id={scheduledTask.Id:B} failed with exception");
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
                    var nextTimeToRun = scheduledTask.CronExpression.GetNextOccurrence(now);
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

        public void ChangeScheduleAndResetScheduler(Guid id, CrontabSchedule cronExpression)
        {
            this.ChangeSchedulesAndResetScheduler(new List<(Guid, CrontabSchedule)> { (id, cronExpression) });
        }

        public void ChangeSchedulesAndResetScheduler(IEnumerable<(Guid Id, CrontabSchedule CrontabSchedule)> scheduleChanges)
        {
            lock (this.threadLock)
            {
                foreach (var scheduleItem in scheduleChanges)
                {
                    this.scheduledTasks.Single(t => t.Id == scheduleItem.Id).SetCronExpression(scheduleItem.CrontabSchedule);
                }
            }

            this.ResetScheduler();
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

        private void RaiseNextEvent(DateTime signalTime, params ScheduledTaskInternal[] scheduledTasks)
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
            this.logger.LogDebug("Stopping...");

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
                    if (this.IsRunning)
                    {
                        this.Stop();
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
