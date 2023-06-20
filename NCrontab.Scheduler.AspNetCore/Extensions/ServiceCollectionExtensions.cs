﻿using System;
using Microsoft.Extensions.Configuration;
using NCrontab.Scheduler;
using NCrontab.Scheduler.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddHostedScheduler(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            // Register services
            serviceCollection.AddScheduler(configuration);

            // Add hosted service
            serviceCollection.AddHostedService<HostedSchedulerService>();
        }

        public static void AddHostedScheduler(this IServiceCollection serviceCollection, Action<SchedulerOptions> options = null)
        {
            // Register services
            serviceCollection.AddScheduler(options);

            // Add hosted service
            serviceCollection.AddHostedService<HostedSchedulerService>();
        }
    }
}