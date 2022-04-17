using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NCrontab.Scheduler.AspNetCore
{
    public class SchedulerHostedService : IHostedService
    {
        private readonly IScheduler scheduler;
        private readonly IEnumerable<IAsyncTask> scheduledTasks;
        private readonly IEnumerable<ITask> scheduledActions;

        public SchedulerHostedService(
            IScheduler scheduler,
            IEnumerable<IAsyncTask> scheduledTasks,
            IEnumerable<ITask> scheduledActions)
        {
            this.scheduler = scheduler;
            this.scheduledTasks = scheduledTasks;
            this.scheduledActions = scheduledActions;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.AddScheduledTasks();
            this.AddScheduledActions();

            this.scheduler.Start();
            return Task.CompletedTask;
        }

        private void AddScheduledTasks()
        {
            if (this.scheduledTasks != null)
            {
                foreach (var scheduledTask in this.scheduledTasks)
                {
                    this.scheduler.AddTask(scheduledTask.CronExpression, (ct) => scheduledTask.RunAsync(ct));
                }
            }
        }
        
        private void AddScheduledActions()
        {
            if (this.scheduledTasks != null)
            {
                foreach (var scheduledAction in this.scheduledActions)
                {
                    this.scheduler.AddTask(scheduledAction.CronExpression, (ct) => scheduledAction.Run(ct));
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.scheduler.Stop();
            return Task.CompletedTask;
        }
    }
}