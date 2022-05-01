using System;

namespace NCrontab.Scheduler.Internals
{
    public class SystemDateTime : IDateTime
    {
        public DateTime Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;
    }
    
    public class DateTimePreciseWrapper : IDateTime
    {
        public DateTime Now => DateTimePrecise.Now;

        public DateTime UtcNow => DateTimePrecise.UtcNow;
    }
}