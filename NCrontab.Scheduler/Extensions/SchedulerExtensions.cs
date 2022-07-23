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
        /// Removes the scheduled <paramref name="task"/>.
        /// </summary>
        /// <param name="taskId">The identifier of the task to be removed.</param>
        /// <returns>True, if task with <paramref name="taskId"/> was found and removed.</returns>
        public static bool RemoveTask(this IScheduler scheduler, ITask task)
        {
            var results = scheduler.RemoveTasks(task);
            return results[0].Removed;
        }

        /// <summary>
        /// Removes the scheduled task with given <paramref name="taskId"/>.
        /// </summary>
        /// <param name="taskId">The identifier of the task to be removed.</param>
        /// <returns>True, if task with <paramref name="taskId"/> was found and removed.</returns>
        public static bool RemoveTask(this IScheduler scheduler, Guid taskId)
        {
            var results = scheduler.RemoveTasks(taskId);
            return results[0].Removed;
        }

        /// <summary>
        /// Removes the scheduled tasks with given <paramref name="taskIds"/>.
        /// </summary>
        /// <param name="taskIds">The identifiers of the tasks to be removed.</param>
        /// <returns>(TaskId, true) for each task that was found and removed. (TaskId, false) for each task that was not found.</returns>
        public static (Guid TaskId, bool Removed)[] RemoveTasks(this IScheduler scheduler, params Guid[] taskIds)
        {
            var tasksToRemove = scheduler.GetTasks()
                .Where(t => taskIds.Contains(t.Id));

            var nonExistingTasks = taskIds
                .Except(tasksToRemove.Select(t => t.Id))
                .Select(d => new NullTask(d));

            var results = scheduler.RemoveTasks(tasksToRemove.Concat(nonExistingTasks).ToArray());
            return results;
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
            return tasks.SelectMany(t => t.CrontabSchedule.GetNextOccurrences(startDateValue, endDateValue).Select(d => (NextOccurrence: d, Task: t)));
        }
    }

    internal class NullTask : ITask
    {
        private Guid taskId;

        public NullTask(Guid taskId)
        {
            this.taskId = taskId;
        }

        public Guid Id => this.taskId;

        public CrontabSchedule CrontabSchedule { get; set; }
    }
}
