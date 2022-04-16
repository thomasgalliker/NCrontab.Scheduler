using System;

namespace NCrontab.Scheduler.Internals
{
    public class DateTimeGenerator
    {
        private int index = 0;
        private readonly TimeSpan[] intervals;
        private readonly int count;
        private DateTime lastDate;

        public DateTimeGenerator(DateTime referenceDate, TimeSpan[] intervals)
        {
            this.lastDate = referenceDate;
            this.intervals = intervals;
            this.count = intervals.Length;
        }

        public DateTime GetNext()
        {
            if (++this.index >= this.count)
            {
                this.index = 0;
            }

            var interval = this.intervals[this.index];
            var value = this.lastDate + interval;
            this.lastDate = value;
            return value;
        }
    }
}