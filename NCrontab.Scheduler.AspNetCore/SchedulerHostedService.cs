using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NCrontab.Scheduler.AspNetCore
{
    /// <summary>
    /// Hosts an <seealso cref="IScheduler"/> instance and automatically starts/stops
    /// when this <seealso cref="SchedulerHostedService"/> is started/stopped.
    /// Call <code>IServiceCollection.AddHostedService<SchedulerHostedService>()</code>
    /// to activate this hosted service.
    /// </summary>
    public class SchedulerHostedService : IHostedService
    {
        private readonly IScheduler scheduler;
        private readonly IEnumerable<IAsyncScheduledTask> scheduledTasks;
        private readonly IEnumerable<IScheduledTask> scheduledActions;

        public SchedulerHostedService(
            IScheduler scheduler,
            IEnumerable<IAsyncScheduledTask> scheduledTasks,
            IEnumerable<IScheduledTask> scheduledActions)
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
                    this.scheduler.AddTask(scheduledTask.CrontabSchedule, (ct) => scheduledTask.RunAsync(ct));
                }
            }
        }
        
        private void AddScheduledActions()
        {
            if (this.scheduledTasks != null)
            {
                foreach (var scheduledAction in this.scheduledActions)
                {
                    this.scheduler.AddTask(scheduledAction.CrontabSchedule, (ct) => scheduledAction.Run(ct));
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