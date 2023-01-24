﻿using Microsoft.Extensions.Logging;
using NCrontab.Scheduler;
using NCrontab.Scheduler.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddScheduler(this IServiceCollection serviceCollection)
        {
            // Register services
            serviceCollection.AddSingleton<ISchedulerFactory>(x => new SchedulerFactory(x));
            serviceCollection.AddSingleton<IScheduler>(x => new Scheduler(x.GetRequiredService<ILogger<Scheduler>>()));

            // Add hosted service
            serviceCollection.AddHostedService<HostedSchedulerService>();
        }
    }
}