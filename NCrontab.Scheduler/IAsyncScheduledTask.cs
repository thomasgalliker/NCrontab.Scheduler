namespace NCrontab.Scheduler
{
    public interface IAsyncScheduledTask : ITask
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
