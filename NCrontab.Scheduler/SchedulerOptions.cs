using System;

namespace NCrontab.Scheduler
{
    public class SchedulerOptions : ISchedulerOptions
    {
        public SchedulerOptions()
        {
            this.DateTimeKind = DateTimeKind.Utc;
            this.Logging = new LoggingOptions();
        }

        /// <inheritdoc />
        public virtual DateTimeKind DateTimeKind { get; set; }

        /// <inheritdoc />
        public virtual LoggingOptions Logging { get; set; }
    }
}