namespace NCrontab.Scheduler
{
    public interface ISchedulerFactory
    {
        IScheduler Create();
    }
}
