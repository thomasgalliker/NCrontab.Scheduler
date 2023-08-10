using System;

namespace NCrontab.Scheduler
{
    public class LoggingOptions
    {
        public LoggingOptions()
        {
            this.DateTimeKind = DateTimeKind.Utc;
            this.LogIdentifier = LogIdentifier.TaskName;
            this.TaskIdFormatter = "B";
        }

        public virtual DateTimeKind DateTimeKind { get; set; }

        /// <summary>
        /// Sets the formatting rule for a task identifier when written to the log output.
        /// This option can improve the readability of the scheduler log output.
        /// It has no impact on functionaly and/or performance of the scheduler.
        /// Default is <c>LogIdentifier.TaskName</c>.
        /// </summary>
        /// <remarks>
        /// Since <see cref="ITask.Name"/> is optional,
        /// <see cref="ITask.Id"/> is used if <see cref="ITask.Name"/> is null or empty.
        /// </remarks>
        public virtual LogIdentifier LogIdentifier { get; set; }

        /// <summary>
        /// Formatter used when logging <see cref="ITask.Id"/>.
        /// Default is <c>"B"</c>.
        /// </summary>
        public virtual string TaskIdFormatter { get; set; }
    }
}