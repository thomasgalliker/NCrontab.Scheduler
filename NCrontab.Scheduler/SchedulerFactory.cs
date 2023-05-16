using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NCrontab.Scheduler
{
    public class SchedulerFactory : ISchedulerFactory
    {
        private readonly IServiceProvider serviceProvider;

        public SchedulerFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        public IScheduler Create()
        {
            return new Scheduler(
                this.serviceProvider.GetRequiredService<ILogger<Scheduler>>(),
                this.serviceProvider.GetRequiredService<IOptions<SchedulerOptions>>());
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        /// <param name="options">The scheduler options.</param>
        public IScheduler Create(ISchedulerOptions options)
        {
            return new Scheduler(
                this.serviceProvider.GetRequiredService<ILogger<Scheduler>>(), 
                options);
        }
    }
}
