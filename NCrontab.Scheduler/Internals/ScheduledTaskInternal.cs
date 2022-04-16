using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler.Internals
{
    public class ScheduledTask
    {

    }

    internal class ScheduledTaskInternal
    {
        public ScheduledTaskInternal(Guid id, CrontabSchedule cronExpression, Action<CancellationToken> action)
        {
            this.Id = id;
            this.CronExpression = cronExpression;
            this.Action = action;
        }

        public ScheduledTaskInternal(Guid id, CrontabSchedule cronExpression, Func<CancellationToken, Task> action)
        {
            this.Id = id;
            this.CronExpression = cronExpression;
            this.ActionTask = action;
        }

        internal void SetCronExpression(CrontabSchedule cronExpression)
        {
            this.CronExpression = cronExpression;
        }

        public Guid Id { get; }

        public CrontabSchedule CronExpression { get; private set; }

        internal Action<CancellationToken> Action { get; }

        internal Func<CancellationToken, Task> ActionTask { get; }
    }
}
