using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public interface IScheduler : IDisposable
    {
        void ChangeScheduleAndResetScheduler(Guid id, CrontabSchedule cronExpression);

        void ChangeSchedulesAndResetScheduler(IEnumerable<(Guid Id, CrontabSchedule CrontabSchedule)> scheduleChanges);

        /// <summary>
        /// Starts the scheduling operations.
        /// This is a non-blocking call.
        /// </summary>
        /// <param name="cancellationToken"></param>
        void Start(CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts the scheduling operations.
        /// This call blocks the further execution.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        bool IsRunning { get; }

        Guid AddTask(string cronExpression, Action<CancellationToken> action);

        Guid AddTask(CrontabSchedule cronExpression, Action<CancellationToken> action);

        void AddTask(Guid id, string cronExpression, Action<CancellationToken> action);

        void AddTask(Guid id, CrontabSchedule cronExpression, Action<CancellationToken> action);

        Guid AddTask(string cronExpression, Func<CancellationToken, Task> func);

        Guid AddTask(CrontabSchedule cronExpression, Func<CancellationToken, Task> func);

        void AddTask(Guid id, string cronExpression, Func<CancellationToken, Task> func);

        void AddTask(Guid id, CrontabSchedule cronExpression, Func<CancellationToken, Task> func);

        /// <summary>
        /// Next event fires if the scheduler triggers the execution
        /// of the next task in the pipeline.
        /// </summary>
        event EventHandler<ScheduledEventArgs> Next;

        void RemoveTask(Guid taskId);

        /// <summary>
        /// All scheduling operations are aborted immediately.
        /// </summary>
        void Stop();
    }
}