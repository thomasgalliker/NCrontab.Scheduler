using System;

namespace NCrontab.Scheduler.Internals
{
    public class SystemDateTime : IDateTime
    {
        public DateTime Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;
    }
}