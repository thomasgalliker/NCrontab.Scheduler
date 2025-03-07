using System;

namespace NCrontab.Scheduler
{
    internal class NullTask : ITask
    {
        private Guid taskId;

        public NullTask(Guid taskId)
        {
            this.taskId = taskId;
        }

        public Guid Id => this.taskId;

        public string Name { get; set; }

        public CrontabSchedule CrontabSchedule { get; set; }
    }
}
