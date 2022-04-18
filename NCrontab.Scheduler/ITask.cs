using System;

namespace NCrontab.Scheduler
{
    public interface ITask
    {
        public Guid Id { get; }

        CrontabSchedule CrontabSchedule { get; set; }
    }
}
