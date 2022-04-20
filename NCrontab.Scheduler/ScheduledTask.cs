using System;
using System.Threading;

namespace NCrontab.Scheduler
{
    public class ScheduledTask : IScheduledTask
    {
        private readonly Action<CancellationToken> action;

        public ScheduledTask(CrontabSchedule cronExpression, Action<CancellationToken> action)
            : this(Guid.NewGuid(), cronExpression, action)
        {
        }

        public ScheduledTask(Guid id, CrontabSchedule cronExpression, Action<CancellationToken> action)
        {
            this.Id = id;
            this.CrontabSchedule = cronExpression;
            this.action = action;
        }
        public Guid Id { get; }

        public CrontabSchedule CrontabSchedule { get; set; }

        public void Run(CancellationToken cancellationToken)
        {
            this.action(cancellationToken);
        }
    }
}
