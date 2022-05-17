using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    /// <summary>
    /// Convenience extension methods for <seealso cref="IScheduler"/>.
    /// </summary>
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
        /// <param name="taskId">The task identifier.</param>
        /// <param name="crontabSchedule">The crontab schedule.</param>
        /// <param name="action">The callback action which is called whenever the <paramref name="crontabSchedule"/> is planned to execute.</param>
        public static void AddTask(this IScheduler scheduler, Guid taskId, CrontabSchedule crontabSchedule, Action<CancellationToken> action)
        {
            var scheduledTask = new ScheduledTask(taskId, crontabSchedule, action);
            scheduler.AddTask(scheduledTask);
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

        /// <summary>
        /// Adds a task to the scheduler.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="crontabSchedule">The crontab schedule.</param>
        /// <param name="action">The callback action which is called whenever the <paramref name="crontabSchedule"/> is planned to execute.</param>
        public static void AddTask(this IScheduler scheduler, Guid taskId, CrontabSchedule crontabSchedule, Func<CancellationToken, Task> action)
        {
            var asyncScheduledTask = new AsyncScheduledTask(taskId, crontabSchedule, action);
            scheduler.AddTask(asyncScheduledTask);
        }

        /// <summary>
        /// Returns the planned next execution dates and tasks.
        /// </summary>
        /// <param name="startDate">The start date (optional).</param>
        /// <param name="endDate">>The end date (optional).</param>
        /// <returns>List of next execution dates and the associated tasks.</returns>
        public static IEnumerable<(DateTime NextOccurrence, IEnumerable<ITask> ScheduledTasks)> GetNextOccurrences(this IScheduler scheduler, DateTime? startDate = null, DateTime? endDate = null)
        {
            var tasks = scheduler.GetTasks();

            var startDateValue = startDate != null ? startDate.Value : DateTime.Now;

            IEnumerable<(DateTime NextOccurrence, ITask Task)> nextOccurrences;

            if (endDate is DateTime endDateValue)
            {
                nextOccurrences = GetNextOccurrences(tasks, startDateValue, endDateValue);
            }
            else
            {
                nextOccurrences = GetNextOccurrences(tasks, startDateValue);
            }

            var tasksGroupedByNextOccurrence = nextOccurrences
                .GroupBy(g => g.NextOccurrence)
                .Select(g => (NextOccurrence: g.Key, ScheduledTasks: g.Select(c => c.Task)));

            return tasksGroupedByNextOccurrence;
        }

        private static IEnumerable<(DateTime NextOccurrence, ITask Task)> GetNextOccurrences(IEnumerable<ITask> tasks, DateTime startDate)
        {
            return tasks.Select(t => (NextOccurrence: t.CrontabSchedule.GetNextOccurrence(startDate), Task: t));
        }

        private static IEnumerable<(DateTime NextOccurrence, ITask Task)> GetNextOccurrences(IEnumerable<ITask> tasks, DateTime startDateValue, DateTime endDateValue)
        {
            return tasks
                .SelectMany(t => t.CrontabSchedule.GetNextOccurrences(startDateValue, endDateValue)
                .Select(d => (NextOccurrence: d, Task: t)));
        }
    }
}
