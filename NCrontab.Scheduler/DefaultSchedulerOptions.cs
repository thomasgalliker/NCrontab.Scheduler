using System;

namespace NCrontab.Scheduler
{
    public class DefaultSchedulerOptions : ISchedulerOptions
    {
        public DefaultSchedulerOptions()
        {
            this.DateTimeKind = DateTimeKind.Utc;
        }

        /// <inheritdoc />
        public DateTimeKind DateTimeKind { get; set; }
    }
}