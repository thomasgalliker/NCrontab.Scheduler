using System;
using Microsoft.Extensions.Logging;
using NCrontab.Scheduler;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddScheduler(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            // Configuration
            serviceCollection.Configure<SchedulerOptions>(configuration);

            // Register services
            serviceCollection.AddScheduler();

            return serviceCollection;
        }

        public static IServiceCollection AddScheduler(this IServiceCollection serviceCollection, Action<SchedulerOptions> options = null)
        {
            // Configuration
            if (options != null)
            {
                serviceCollection.Configure(options);
            }
   
            // Register services
            serviceCollection.AddSingleton<ISchedulerFactory>(x => new SchedulerFactory(x));
            serviceCollection.AddSingleton<IScheduler>(x =>
            {
                return new Scheduler(
                    x.GetRequiredService<ILogger<Scheduler>>(),
                    x.GetRequiredService<IOptions<SchedulerOptions>>());
            });

            return serviceCollection;
        }
    }
}