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

            // Create instance of Scheduler
            // or inject IScheduler using dependency injection.
            IScheduler scheduler = new Scheduler();

            // Subscribe Next event to get notified
            // for all tasks that are executed.
            scheduler.Next += OnSchedulerNext;

            // Add tasks with different cron schedules and actions.
            scheduler.AddTask(
                cronExpression: CrontabSchedule.Parse("* * * * *"), 
                action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every minutes"); });

            scheduler.AddTask(
                cronExpression: CrontabSchedule.Parse("*/2 * * * *"), 
                action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every second minutes"); });

            scheduler.AddTask(
                cronExpression: CrontabSchedule.Parse("*/3 * * * *"), 
                action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every third minutes"); });
           
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