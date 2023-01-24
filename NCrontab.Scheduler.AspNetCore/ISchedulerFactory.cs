namespace NCrontab.Scheduler.AspNetCore
{
    public interface ISchedulerFactory
    {
        IScheduler Create();
    }
}
