﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public interface IScheduler : IDisposable
    {
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
        /// Returns the <seealso cref="ITask"/> for the given <paramref name="taskId"/>.
        /// If the task cannot be found, null is returned.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <returns><seealso cref="ITask"/> or null (if not found).</returns>
        ITask GetTaskById(Guid taskId);

        /// <summary>
        /// Updates the schedule of the <paramref name="scheduledTask"/>.
        /// </summary>
        /// <param name="scheduledTask"></param>
        void UpdateTask(ITask scheduledTask);

        /// <summary>
        /// Next event fires if the scheduler triggers the execution
        /// of the next task in the pipeline.
        /// </summary>
        event EventHandler<ScheduledEventArgs> Next;

        /// <summary>
        /// Removes the scheduled task with given <paramref name="taskId"/>.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <returns>True, if task with <paramref name="taskId"/> was found and removed.</returns>
        bool RemoveTask(Guid taskId);

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