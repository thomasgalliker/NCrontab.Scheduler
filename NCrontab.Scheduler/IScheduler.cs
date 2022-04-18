using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public interface IScheduler : IDisposable
    {
        void ChangeScheduleAndResetScheduler(Guid taskId, CrontabSchedule cronExpression);

        void ChangeSchedulesAndResetScheduler(IEnumerable<(Guid TaskId, CrontabSchedule CrontabSchedule)> scheduleChanges);

        /// <summary>
        /// Starts the scheduling operations.
        /// This call blocks the further execution.
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Indicates if the scheduler is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Adds a task to the scheduler.
        /// </summary>
        /// <param name="scheduledTask">The scheduled task (synchronous action).</param>
        void AddTask(IScheduledTask scheduledTask);

        /// <summary>
        /// Adds a task to the scheduler.
        /// </summary>
        /// <param name="scheduledTask">The scheduled task (asynchronous action).</param>
        void AddTask(IAsyncScheduledTask scheduledTask);

        /// <summary>
        /// Next event fires if the scheduler triggers the execution
        /// of the next task in the pipeline.
        /// </summary>
        event EventHandler<ScheduledEventArgs> Next;

        /// <summary>
        /// Removes the scheduled task with given <paramref name="taskId"/>.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        void RemoveTask(Guid taskId);

        /// <summary>
        /// Removes all scheduled tasks.
        /// </summary>
        void RemoveAllTasks();

        /// <summary>
        /// All scheduling operations are aborted immediately.
        /// </summary>
        void Stop();
    }
}