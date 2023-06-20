using System.Globalization;
using Microsoft.Extensions.Logging;

namespace NCrontab.Scheduler.ConsoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine(
                $"Scheduler ConsoleApp version {typeof(Program).Assembly.GetName().Version} {Environment.NewLine}" +
                $"Copyright(C) superdev GmbH. All rights reserved.{Environment.NewLine}");

            var dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat;
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddDebug()
                    .AddSimpleConsole(c =>
                    {
                        c.TimestampFormat = $"{dateTimeFormat.ShortDatePattern} {dateTimeFormat.LongTimePattern} ";
                    })

                    // Set the default log level to LogLevel.Debug
                    // in order to get more detailed information from Scheduler
                    .SetMinimumLevel(LogLevel.Debug);
            });

            // Create instance of Scheduler manually
            // or inject IScheduler using dependency injection.
            ILogger<Scheduler> logger = loggerFactory.CreateLogger<Scheduler>();
            ISchedulerOptions schedulerOptions = new SchedulerOptions
            {
                DateTimeKind = DateTimeKind.Utc
            };
            IScheduler scheduler = new Scheduler(logger, schedulerOptions);

            // Subscribe Next event to get notified
            // for all tasks that are executed.
            scheduler.Next += OnSchedulerNext;

            // Add tasks with different cron schedules and actions. 
            scheduler.AddTask(
                crontabSchedule: CrontabSchedule.Parse("* * * * *"),
                action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every minutes"); });

            scheduler.AddTask(
                crontabSchedule: CrontabSchedule.Parse("*/2 * * * *"),
                action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every second minute"); });

            scheduler.AddTask(
                crontabSchedule: CrontabSchedule.Parse("0 * * * *"),
                action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every hour"); });

            scheduler.AddTask(
                crontabSchedule: CrontabSchedule.Parse("0 0 * * *"),
                action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every day at midnight"); });

            scheduler.AddTask(
                crontabSchedule: CrontabSchedule.Parse("0 0 1 1 *"),
                action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs on Januar 1 every year"); });

            scheduler.AddTask(
              crontabSchedule: CrontabSchedule.Parse("0 3 29 10 *"),
              action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs at a very specific date/time"); });

            // Finally, start the scheduler and observe the action callbacks
            // as well as the Next event handler.
            await scheduler.StartAsync();

            Console.ReadLine();
        }

        private static void OnSchedulerNext(object? sender, ScheduledEventArgs e)
        {
            Console.WriteLine(
                $"{DateTime.Now:O} -> OnSchedulerNext with TaskIds={string.Join(", ", e.TaskIds.Select(i => $"{i:B}"))}");
        }
    }
}