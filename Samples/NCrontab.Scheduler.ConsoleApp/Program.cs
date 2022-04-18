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

            Console.WriteLine(
                $"Wait until the first task is scheduled for execution...{Environment.NewLine}");

            var dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat;
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddDebug()
                    .AddSimpleConsole(c =>
                    {
                        c.TimestampFormat = $"{dateTimeFormat.ShortDatePattern} {dateTimeFormat.LongTimePattern} ";
                    });
            });

            // Create instance of Scheduler with or without ILogger<Scheduler>
            // or inject IScheduler using dependency injection.
            var schedulerLogger = loggerFactory.CreateLogger<Scheduler>();
            IScheduler scheduler = new Scheduler(schedulerLogger);

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

            // Finally, start the scheduler and observe the action callbacks
            // as well as the Next event handler.
            await scheduler.StartAsync();

            Console.ReadLine();
        }

        private static void OnSchedulerNext(object? sender, ScheduledEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now:O} -> OnSchedulerNext with TaskIds={string.Join(", ", e.TaskIds.Select(i => $"{i:B}"))}");
        }
    }
}