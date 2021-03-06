using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NCrontab.Scheduler.AspNetCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddScheduler(this IServiceCollection serviceCollection)
        {
            // Register services
            serviceCollection.AddSingleton<IScheduler>(x => new Scheduler(x.GetRequiredService<ILogger<Scheduler>>()));

            // Add hosted service
            serviceCollection.AddHostedService<HostedSchedulerService>();
        }
    }
}