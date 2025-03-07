using System.Threading;
using System;

namespace NCrontab.Scheduler
{
    /// <summary>
    /// Base class for <see cref="ITask"/>.
    /// </summary>
    public abstract class TaskBase : ITask
    {
        protected TaskBase(Guid id, CrontabSchedule crontabSchedule)
            : this(id, null, crontabSchedule)
        {
        }
        
        protected TaskBase(string name, CrontabSchedule crontabSchedule)
            : this(Guid.NewGuid(), name, crontabSchedule)
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

        public override string ToString()
        {
            return $"{this.GetType().Name}: {this.Name ?? this.Id.ToString()}";
        }
    }
}