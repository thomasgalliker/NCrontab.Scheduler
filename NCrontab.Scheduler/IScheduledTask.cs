using System.Threading;

namespace NCrontab.Scheduler
{
    public interface IScheduledTask : ITask
    {
        void Run(CancellationToken cancellationToken);
    }
}
