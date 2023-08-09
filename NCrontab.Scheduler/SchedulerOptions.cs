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
        public DateTimeKind DateTimeKind { get; set; }

        /// <inheritdoc />
        public LoggingOptions Logging { get; set; }
    }
}