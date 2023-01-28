using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NCrontab.Scheduler
{
    public class SchedulerFactory : ISchedulerFactory
    {
        private readonly IServiceProvider serviceProvider;

        public SchedulerFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IScheduler Create()
        {
            return new Scheduler(logger: this.serviceProvider.GetRequiredService<ILogger<Scheduler>>());
        }
    }
}
