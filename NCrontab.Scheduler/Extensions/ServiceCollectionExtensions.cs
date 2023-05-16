using System;
using Microsoft.Extensions.Logging;
using NCrontab.Scheduler;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddScheduler(this IServiceCollection serviceCollection, Action<SchedulerOptions> configureOptions = null)
        {
            // Configuration
            if (configureOptions != null)
            {
                serviceCollection.Configure(configureOptions);
            }

            // Register services
            serviceCollection.AddSingleton<ISchedulerFactory>(x => new SchedulerFactory(x));
            serviceCollection.AddSingleton<IScheduler>(x =>
            {
                return new Scheduler(
                    x.GetRequiredService<ILogger<Scheduler>>(),
                    x.GetRequiredService<IOptions<SchedulerOptions>>());
            });
        }
    }
}