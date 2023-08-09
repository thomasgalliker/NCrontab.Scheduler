using System;

namespace NCrontab.Scheduler
{
    /// <summary>
    /// Abstraction for any kind of task that can be scheduled with a <see cref="Scheduler"/> instance.
    /// See also:
    /// <seealso cref="ScheduledTask"/>
    /// <seealso cref="AsyncScheduledTask"/>
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Unique identifier of a task.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Display name of a task.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The cron schedule expressions.
        /// </summary>
        CrontabSchedule CrontabSchedule { get; set; }
    }
}
