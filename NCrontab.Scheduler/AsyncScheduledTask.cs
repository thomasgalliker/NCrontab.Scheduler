using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public class AsyncScheduledTask : TaskBase, IAsyncScheduledTask
    {
        private readonly Func<CancellationToken, Task> action;

        public AsyncScheduledTask(string cronExpression, Func<CancellationToken, Task> action)
            : base(cronExpression)
        {
            this.action = action;
        }

        public AsyncScheduledTask(CrontabSchedule crontabSchedule, Func<CancellationToken, Task> action)
            : base(crontabSchedule)
        {
            this.action = action;
        }
        
        public AsyncScheduledTask(Guid id, CrontabSchedule crontabSchedule, Func<CancellationToken, Task> action)
            : base(id, crontabSchedule)
        {
            this.action = action;
        }

        public AsyncScheduledTask(string name, CrontabSchedule crontabSchedule, Func<CancellationToken, Task> action)
            : base(name, crontabSchedule)
        {
            this.action = action;
        }

        public AsyncScheduledTask(Guid id, string name, CrontabSchedule crontabSchedule, Func<CancellationToken, Task> action)
            : base(id, name, crontabSchedule)
        {
            this.action = action;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return this.action(cancellationToken);
        }
    }
}
