using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public static class SchedulerExtensions
    {
        /// <summary>
        /// Starts the scheduling operations.
        /// This is a non-blocking call.
        /// </summary>
        public static void Start(this IScheduler scheduler, CancellationToken cancellationToken = default)
        {
            Task.Run(() => scheduler.StartAsync(cancellationToken));
        }

        /// <summary>
        /// Adds a task to the scheduler.
        /// </summary>
        /// <param name="cronExpression">The cron expression.</param>
        /// <param name="action">The callback action which is called whenever the <paramref name="cronExpression"/> is planned to execute.</param>
        /// <returns>The task identifier.</returns>
        public static Guid AddTask(this IScheduler scheduler, string cronExpression, Action<CancellationToken> action)
        {
            var crontabSchedule = CrontabSchedule.Parse(cronExpression);
            return scheduler.AddTask(crontabSchedule, action);
        }

        /// <summary>
        /// Adds a task to the scheduler.
        /// </summary>
        /// <param name="crontabSchedule">The crontab schedule.</param>
        /// <param name="action">The callback action which is called whenever the <paramref name="crontabSchedule"/> is planned to execute.</param>
        /// <returns>The task identifier.</returns>
        public static Guid AddTask(this IScheduler scheduler, CrontabSchedule crontabSchedule, Action<CancellationToken> action)
        {
            var taskId = Guid.NewGuid();

            scheduler.AddTask(taskId, crontabSchedule, action);

            return taskId;
        }

        /// <summary>
        /// Adds a task to the scheduler.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="cronExpression">The cron expression.</param>
        /// <param name="action">The callback action which is called whenever the <paramref name="cronExpression"/> is planned to execute.</param>
        public static void AddTask(this IScheduler scheduler, Guid taskId, string cronExpression, Action<CancellationToken> action)
        {
            var crontabSchedule = CrontabSchedule.Parse(cronExpression);
            scheduler.AddTask(taskId, crontabSchedule, action);
        }

        /// <summary>
        /// Adds a task to the scheduler.
        /// </summary>
        /// <param name="cronExpression">The cron expression.</param>
        /// <param name="action">The callback action which is called whenever the <paramref name="cronExpression"/> is planned to execute.</param>
        /// <returns>The task identifier.</returns>
        public static Guid AddTask(this IScheduler scheduler, string cronExpression, Func<CancellationToken, Task> action)
        {
            var crontabSchedule = CrontabSchedule.Parse(cronExpression);
            return scheduler.AddTask(crontabSchedule, action);
        }

        /// <summary>
        /// Adds a task to the scheduler.
        /// </summary>
        /// <param name="crontabSchedule">The crontab schedule.</param>
        /// <param name="action">The callback action which is called whenever the <paramref name="crontabSchedule"/> is planned to execute.</param>
        /// <returns>The task identifier.</returns>
        public static Guid AddTask(this IScheduler scheduler, CrontabSchedule crontabSchedule, Func<CancellationToken, Task> action)
        {
            var taskId = Guid.NewGuid();

            scheduler.AddTask(taskId, crontabSchedule, action);

            return taskId;
        }

        /// <summary>
        /// Adds a task to the scheduler.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="cronExpression">The cron expression.</param>
        /// <param name="action">The callback action which is called whenever the <paramref name="cronExpression"/> is planned to execute.</param>
        public static void AddTask(this IScheduler scheduler, Guid taskId, string cronExpression, Func<CancellationToken, Task> action)
        {
            var crontabSchedule = CrontabSchedule.Parse(cronExpression);
            scheduler.AddTask(taskId, crontabSchedule, action);
        }
    }
}
