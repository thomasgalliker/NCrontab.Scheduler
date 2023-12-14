namespace NCrontab.Scheduler
{
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