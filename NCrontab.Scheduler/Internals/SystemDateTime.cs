using System;

namespace NCrontab.Scheduler.Internals
{
    public class SystemDateTime : IDateTime
    {
        DateTime IDateTime.Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;
    }
}