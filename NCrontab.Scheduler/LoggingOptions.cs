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

        public DateTimeKind DateTimeKind { get; set; }

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
        public LogIdentifier LogIdentifier { get; set; }

        /// <summary>
        /// Formatter used when logging <see cref="ITask.Id"/>.
        /// Default is <c>"B"</c>.
        /// </summary>
        public string TaskIdFormatter { get; set; }
    }

    public enum LogIdentifier
    {
        /// <summary>
        /// Use <see cref="ITask.Id"/> in the log messages.
        /// </summary>
        TaskId,

        /// <summary>
        /// Use <see cref="ITask.Name"/> in the log messages.
        /// </summary>
        TaskName,

        /// <summary>
        /// Use <see cref="ITask.Id"/> and <see cref="ITask.Name"/> (if available) in the log messages.
        /// </summary>
        TaskIdAndName,

        /// <summary>
        /// Use <see cref="ITask.Name"/> (if available) and <see cref="ITask.Id"/> in the log messages.
        /// </summary>
        TaskNameAndId,
    }
}