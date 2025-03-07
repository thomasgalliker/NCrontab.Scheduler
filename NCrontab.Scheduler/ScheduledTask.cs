using System;
using System.Threading;

namespace NCrontab.Scheduler
{
    public class ScheduledTask : TaskBase, IScheduledTask
    {
        private readonly Action<CancellationToken> action;

        public ScheduledTask(string cronExpression, Action<CancellationToken> action = null)
            : this(CrontabSchedule.Parse(cronExpression), action)
        {
        }

        public ScheduledTask(CrontabSchedule crontabSchedule, Action<CancellationToken> action = null)
            : this(Guid.NewGuid(), crontabSchedule, action)
        {
        }

        public ScheduledTask(Guid id, CrontabSchedule crontabSchedule, Action<CancellationToken> action = null)
            : this(id, null, crontabSchedule, action)
        {
            this.action = action;
        }
        
        public ScheduledTask(string name, CrontabSchedule crontabSchedule, Action<CancellationToken> action = null)
            : this(Guid.NewGuid(), name, crontabSchedule, action)
        {
            this.action = action;
        }
        
        public ScheduledTask(Guid id, string name, CrontabSchedule crontabSchedule, Action<CancellationToken> action = null)
            : base(id, name, crontabSchedule)
        {
            this.action = action;
        }

        public void Run(CancellationToken cancellationToken)
        {
            this.action?.Invoke(cancellationToken);
        }
    }
}
