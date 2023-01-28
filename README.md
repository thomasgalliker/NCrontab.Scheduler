# NCrontab.Scheduler
[![Version](https://img.shields.io/nuget/v/NCrontab.Scheduler.svg)](https://www.nuget.org/packages/NCrontab.Scheduler)  [![Downloads](https://img.shields.io/nuget/dt/NCrontab.Scheduler.svg)](https://www.nuget.org/packages/NCrontab.Scheduler)

NCrontab.Scheduler is a simple, open source task scheduling system that can be used in any .NET application.
The main component of this project is a thread-safe scheduler which facilitates very basic scheduling operations like adding, remove or changing task schedules.
NCrontab.Scheduler is built on top of NCrontab.

## Download and Install NCrontab.Scheduler
This library is available on NuGet: https://www.nuget.org/packages/NCrontab.Scheduler
Use the following command to install NCrontab.Scheduler using NuGet package manager console:

    PM> Install-Package NCrontab.Scheduler

You can use this library in any .NET Standard or .NET Core project.

## ASP.NET Core Integration
In ASP.NET Core projects, use following NuGet package: https://www.nuget.org/packages/NCrontab.Scheduler.AspNetCore
Use the following command to install NCrontab.Scheduler.AspNetCore using NuGet package manager console:

    PM> Install-Package NCrontab.Scheduler.AspNetCore

You can use this library in any ASP.NET Core project which is compatible to .NET Core 3.1 and higher.

## API Usage
### Creating a Scheduler
`Scheduler` implements the main scheduler operations.

#### Create new Scheduler instance
You can either create a new instance of Scheduler manually:
```C#
IScheduler scheduler = new Scheduler();
```

#### Access static Scheduler instance
Alternatively, you can access the provided singleton instance `Scheduler.Current`.

#### Inject Scheduler using dependency injection
Alternatively, you can register/resolve `IScheduler` in Microsoft's DI framework `Microsoft.Extensions.DependencyInjection`.
```C#
serviceCollection.AddScheduler();
```

This `AddScheduler` call registers `IScheduler` and `ISchedulerFactory` as singleton services which can now be injected in your code.
If you prefer to have multiple instances of `IScheduler` across your code, inject `ISchedulerFactory` instead and use the Create method to create new instances of `IScheduler`.

### Add Scheduled Tasks
Use method `AddTask` with all the provided convenience overloads to add tasks to the scheduler.
A task is composed of a cron pattern which specifies the recurrance interval and an action (for synchronous callbacks) or a task (for asynchronous callbacks).

Tasks can be added to the scheduler either before or after the scheduler has been started.

```C#
scheduler.AddTask(
    cronExpression: CrontabSchedule.Parse("* * * * *"),
    action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every minutes"); });

scheduler.AddTask(
    cronExpression: CrontabSchedule.Parse("*/2 * * * *"),
    action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every second minute"); });

scheduler.AddTask(
    cronExpression: CrontabSchedule.Parse("0 * * * *"),
    action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every hour"); });
            
scheduler.AddTask(
    cronExpression: CrontabSchedule.Parse("0 0 * * *"),
    action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs every day at midnight"); });
            
scheduler.AddTask(
    cronExpression: CrontabSchedule.Parse("0 0 1 1 *"),
    action: ct => { Console.WriteLine($"{DateTime.Now:O} -> Task runs on Januar 1 every year"); });    
```
A very helpful resource for creating cron expression is https://crontab.guru.

### Starting and Stopping
Use method `IScheduler.StartAsync` to start the scheduler operations.
This method can be awaited which blocks all further calls until all scheduled tasks have been canceled or removed.
Use `IScheduler.Start` if you prefer to start the scheduler without blocking the current execution path of your program.
If the scheduler is started without having added any tasks, it just waits (and blocks) until tasks are added.

Scheduled tasks can be added at runtime. If the scheduler is started without having added tasks beforehand, it logs this info: `Scheduler is waiting for tasks. Use AddTask methods to add tasks.`

`IScheduler.Stop` attempts to cancel all scheduled tasks immediately.

## About Scheduling
Real-time systems are systems that are required to respond to an external event in some timely manner.
"Timely manner" is often misinterpreted as "very fast".
However, for many applications this just means the system needs to react in a specific time. It can be seconds, minutes, hours or even years.
Schedulers such as NCrontab.Scheduler can help to satisfy such timing constraints. 

### Scheduler Algorithm
NCrontab.Scheduler is a classic implementation of the Earliest Deadline First (EDF) algorithm.
The EDF algorithm will always schedule the task(s) whose deadline is soonest.

The scheduler provides a dynamic prioritization of tasks by evaluating the cron expressions of the tasks.
Each scheduling iteration attempts to find the task with the earliest possible execution date/time.

If tasks are added or removed while waiting for the next execution, the earliest possible execution date/time is re-evaluated.

### Task Overrun Condition
Task overrun conditions can happen if a scheduled task's execution time exceeds the limit of one minute.
Task overrun can lead to situations where other tasks miss their projected next execution time.
Consequently, each of other tasks start missing their deadlines one after the other in sequence (domino effect). This is a classic EDF scheduling problem and can lead to dangerous situations!

### Thread-Safety
All scheduler operations are kept thread-safe. Adding and removing tasks as well as starting and stopping the scheduler can be done concurrently.

### Task Isolation
Each task run is isolated from all other scheduled tasks. The success or failure of execution does not have any negative side effect on other scheduled tasks.

## License
This project is Copyright &copy; 2023 [Thomas Galliker](https://ch.linkedin.com/in/thomasgalliker). Free for non-commercial use. For commercial use please contact the author.

