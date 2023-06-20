using System;

namespace NCrontab.Scheduler
{
    public class SchedulerOptions : ISchedulerOptions
    {
        public SchedulerOptions()
        {
            this.DateTimeKind = DateTimeKind.Utc;
        }

        /// <inheritdoc />
        public DateTimeKind DateTimeKind { get; set; }
    }
}