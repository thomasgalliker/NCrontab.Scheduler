# NCrontab.Scheduler
[![Version](https://img.shields.io/nuget/v/NCrontab.Scheduler.svg)](https://www.nuget.org/packages/NCrontab.Scheduler)  [![Downloads](https://img.shields.io/nuget/dt/NCrontab.Scheduler.svg)](https://www.nuget.org/packages/NCrontab.Scheduler)

<img src="https://raw.githubusercontent.com/thomasgalliker/NCrontab.Scheduler/develop/logo.png" width="100" height="100" alt="NCrontab.Scheduler" align="right"></img>

NCrontab.Scheduler is a simple, open source task scheduling system that can be used in any .NET application.
The main component of this project is a thread-safe scheduler which facilitates very basic scheduling operations like adding, remove or changing task schedules.
NCrontab.Scheduler is built on top of NCrontab.

### Download and Install NCrontab.Scheduler
This library is available on NuGet: https://www.nuget.org/packages/NCrontab.Scheduler
Use the following command to install NCrontab.Scheduler using NuGet package manager console:

    PM> Install-Package NCrontab.Scheduler

You can use this library in any .NET Standard or .NET Core project.

### API Usage
#### Creating a Scheduler
`Scheduler` implements the main scheduler operations.
You can either create a new instance of Scheduler manually:
```C#
IScheduler scheduler = new Scheduler();
```
Alternatively, you can access the provided singleton instance `Scheduler.Current` or register/resolve `Scheduler` and `IScheduler` in your dependency injection framework.
```C#
serviceCollection.AddSingleton<IScheduler>(x => new Scheduler(x.GetRequiredService<ILogger<Scheduler>>()));
```

#### Add Scheduled Tasks
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

#### Starting and Stopping
Use method `StartAsync` to start the scheduler operations. This method can be awaited which blocks all further calls until all scheduled tasks have been canceled or removed.
Use `Start` if you prefer to start the scheduler without blocking the current execution path of your program.
If the scheduler is started without having added any tasks, it just waits (and blocks) until tasks are added.

`Stop` attempts to cancel all scheduled tasks immediately.

### About Scheduling
Real-time systems are systems that are required to respond to an external event in some timely manner.
"Timely manner" is often misinterpreted as "very fast".
However, for many applications this just means the system needs to react in a specific time. It can be seconds, minutes, hours or even years.
Schedulers such as NCrontab.Scheduler can help to satisfy such timing constraints. 

#### Scheduler Algorithm
NCrontab.Scheduler is a classic implementation of the Earliest Deadline First (EDF) algorithm.
The EDF algorithm will always schedule the task(s) whose deadline is soonest.

The scheduler provides a dynamic prioritization of tasks by evaluating the cron expressions of the tasks.
Each scheduling iteration attempts to find the task with the earliest possible execution date/time.

If tasks are added or removed during while waiting for the next execution, the evaluation of the earliest possible execution date/time is re-evaluated.

#### Thread-Safety
All scheduler operations are kept thread-safe. Adding and removing tasks as well as starting and stopping the scheduler can be done concurrently.

#### Task Isolation
Each task run is isolated from all other scheduled tasks. The success or failure of execution does not have any negative side effect on other scheduled tasks.

### License
This project is Copyright &copy; 2022 [Thomas Galliker](https://ch.linkedin.com/in/thomasgalliker). Free for non-commercial use. For commercial use please contact the author.

