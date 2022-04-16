using System;

namespace NCrontab.Scheduler.Internals
{
    public interface IDateTime
    {
        DateTime Now { get; }

        DateTime UtcNow { get; }
    }
}