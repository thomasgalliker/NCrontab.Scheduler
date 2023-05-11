using System;
namespace NCrontab.Scheduler
{
    public interface ISchedulerOptions
    {
        /// <summary>
        /// Specifies whether the scheduler uses local time or UTC for its operations.
        /// Default is DateTimeKind.Utc.
        /// </summary>
        public DateTimeKind DateTimeKind { get; set; }
    }
}