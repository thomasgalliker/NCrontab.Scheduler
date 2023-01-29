using Microsoft.Extensions.DependencyInjection;
using NCrontab.Scheduler.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddHostedScheduler(this IServiceCollection serviceCollection)
        {
            // Register services
            serviceCollection.AddScheduler();

            // Add hosted service
           serviceCollection.AddHostedService<HostedSchedulerService>();
        }
    }
}