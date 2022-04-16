using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCrontab.Scheduler.Internals;

namespace NCrontab.Scheduler.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddScheduler(this IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IScheduler>(x => new Scheduler(x.GetRequiredService<ILogger<Scheduler>>()));
        }
    }
}