using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NCrontab.Scheduler.AspNetCore
{
    /// <summary>
    /// Hosts an <seealso cref="IScheduler"/> instance and automatically starts/stops
    /// when this <seealso cref="HostedSchedulerService"/> is started/stopped.
    /// Call <code>IServiceCollection.AddHostedService<SchedulerHostedService>()</code>
    /// to activate this hosted service.
    /// </summary>
    public class HostedSchedulerService : IHostedService
    {
        private readonly IScheduler scheduler;
        private readonly IEnumerable<IScheduledTask> scheduledTasks;
        private readonly IEnumerable<IAsyncScheduledTask> asyncScheduledTasks;

        public HostedSchedulerService(
            IScheduler scheduler,
            IEnumerable<IScheduledTask> scheduledTasks,
            IEnumerable<IAsyncScheduledTask> asyncScheduledTasks)
        {
            this.scheduler = scheduler;
            this.scheduledTasks = scheduledTasks;
            this.asyncScheduledTasks = asyncScheduledTasks;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.AddScheduledTasks();
            this.AddAsyncScheduledTasks();

            this.scheduler.Start();
            return Task.CompletedTask;
        }

        private void AddAsyncScheduledTasks()
        {
            if (this.asyncScheduledTasks != null)
            {
                foreach (var scheduledTask in this.asyncScheduledTasks)
                {
                    this.scheduler.AddTask(scheduledTask);
                }
            }
        }
        
        private void AddScheduledTasks()
        {
            if (this.asyncScheduledTasks != null)
            {
                foreach (var scheduledTask in this.scheduledTasks)
                {
                    this.scheduler.AddTask(scheduledTask);
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