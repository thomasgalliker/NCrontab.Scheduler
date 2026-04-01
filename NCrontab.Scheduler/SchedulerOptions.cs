using System;

namespace NCrontab.Scheduler
{
    /// <summary>
    /// Configuration options for <see cref="Scheduler"/>.
    /// </summary>
    public class SchedulerOptions
    {
        public SchedulerOptions()
        {
            this.DateTimeKind = DateTimeKind.Utc;
            this.TaskExecutionMode = TaskExecutionMode.Sequential;
            this.Logging = new LoggingOptions();
        }

        /// <summary>
        /// Specifies whether the scheduler parses cron expressions as local time or UTC.
        /// Default is <see cref="DateTimeKind.Utc"/>.
        /// </summary>
        /// <remarks>
        /// Internal scheduler operations always run with UTC in order to have a
        /// consistent behavior across daylight savings time changes.
        /// Log messages are formatted with UTC timestamps too.
        /// </remarks>
        public virtual DateTimeKind DateTimeKind { get; set; }

        /// <summary>
        /// Controls whether tasks due at the same instant are executed sequentially or concurrently.
        /// Default is <see cref="NCrontab.Scheduler.TaskExecutionMode.Sequential"/>.
        /// </summary>
        public virtual TaskExecutionMode TaskExecutionMode { get; set; }

        /// <summary>
        /// Logging-related options for <see cref="Scheduler"/>.
        /// </summary>
        public virtual LoggingOptions Logging { get; set; }
    }
}
