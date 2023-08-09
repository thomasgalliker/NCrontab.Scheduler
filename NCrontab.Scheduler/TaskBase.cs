using System.Threading;
using System;

namespace NCrontab.Scheduler
{
    /// <summary>
    /// Base class for <see cref="ITask"/>.
    /// </summary>
    public abstract class TaskBase : ITask
    {
        protected TaskBase(string cronExpression)
            : this(Guid.NewGuid(), CrontabSchedule.Parse(cronExpression))
        {
        }

        protected TaskBase(CrontabSchedule crontabSchedule)
            : this(id: Guid.NewGuid(), name: null, crontabSchedule)
        {
        }

        protected TaskBase(Guid id, CrontabSchedule crontabSchedule)
            : this(id, name: null, crontabSchedule)
        {
        }

        protected TaskBase(string name, CrontabSchedule crontabSchedule)
            : this(id: Guid.NewGuid(), name, crontabSchedule)
        {
        }

        protected TaskBase(Guid id, string name, CrontabSchedule crontabSchedule)
        {
            this.Id = id;
            this.Name = name;
            this.CrontabSchedule = crontabSchedule;
        }

        public Guid Id { get; }

        public string Name { get; set; }

        public CrontabSchedule CrontabSchedule { get; set; }

    }
}