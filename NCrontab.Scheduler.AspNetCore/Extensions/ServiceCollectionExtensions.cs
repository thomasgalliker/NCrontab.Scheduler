using System;
using NCrontab.Scheduler;
using NCrontab.Scheduler.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddHostedScheduler(this IServiceCollection serviceCollection, Action<SchedulerOptions> configureOptions = null)
        {
            // Register services
            serviceCollection.AddScheduler(configureOptions);

            // Add hosted service
            serviceCollection.AddHostedService<HostedSchedulerService>();
        }
    }
}