using System.Threading;

namespace NCrontab.Scheduler
{
    public interface ITask
    {
        string CronExpression { get; }

        void Run(CancellationToken cancellationToken);
    }
}
