using System;
namespace NCrontab.Scheduler
{
    public interface ISchedulerOptions
    {
        /// <summary>
        /// Specifies whether the scheduler parses cron expressions as local time or UTC.
        /// Default is DateTimeKind.Utc.
        /// </summary>
        /// <remarks>
        /// Internal scheduler operations always run with UTC in order to have a
        /// consistent behavior across daylight savings time changes.
        /// Log messages are formatted with UTC timestamps too.</remarks>
        public DateTimeKind DateTimeKind { get; set; }
    }
}