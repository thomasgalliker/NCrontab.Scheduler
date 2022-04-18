using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public class AsyncScheduledTask : IAsyncScheduledTask
    {
        private readonly Func<CancellationToken, Task> action;

        public AsyncScheduledTask(Guid id, CrontabSchedule crontabSchedule, Func<CancellationToken, Task> action)
        {
            this.Id = id;
            this.CrontabSchedule = crontabSchedule;
            this.action = action;
        }

        public Guid Id { get; }

        public CrontabSchedule CrontabSchedule { get; set; }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return this.action(cancellationToken);
        }


    }
}
