﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public class AsyncScheduledTask : TaskBase, IAsyncScheduledTask
    {
        private readonly Func<CancellationToken, Task> task;

        public AsyncScheduledTask(string cronExpression, Func<CancellationToken, Task> task = null)
            : this(CrontabSchedule.Parse(cronExpression), task)
        {
        }

        public AsyncScheduledTask(CrontabSchedule crontabSchedule, Func<CancellationToken, Task> task = null)
            : this(Guid.NewGuid(), crontabSchedule, task)
        {
        }

        public AsyncScheduledTask(Guid id, CrontabSchedule crontabSchedule, Func<CancellationToken, Task> task = null)
            : this(id, null, crontabSchedule, task)
        {
            this.task = task;
        }

        public AsyncScheduledTask(string name, CrontabSchedule crontabSchedule, Func<CancellationToken, Task> task = null)
            : this(Guid.NewGuid(), name, crontabSchedule, task)
        {
            this.task = task;
        }

        public AsyncScheduledTask(Guid id, string name, CrontabSchedule crontabSchedule, Func<CancellationToken, Task> task = null)
          : base(id, name, crontabSchedule)
        {
            this.task = task;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return this.task != null ? this.task(cancellationToken) : Task.CompletedTask;
        }
    }
}
