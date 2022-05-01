using System;

namespace NCrontab.Scheduler.Extensions
{
    internal static class TimeSpanExtensions
    {
        internal static TimeSpan RoundUp(this TimeSpan timeSpan, TimeSpan maxRounding)
        {
            var totalSeconds = Math.Min(Math.Ceiling(timeSpan.TotalSeconds), (timeSpan + maxRounding).TotalSeconds);
            var roundedTimeSpan = TimeSpan.FromTicks((long)(totalSeconds * TimeSpan.TicksPerSecond));
            return roundedTimeSpan;
        }
    }
}