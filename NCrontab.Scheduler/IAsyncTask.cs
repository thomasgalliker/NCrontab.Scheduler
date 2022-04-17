using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public interface IAsyncTask
    {
        string CronExpression { get; }

        Task RunAsync(CancellationToken cancellationToken);
    }

    public interface ITask
    {
        string CronExpression { get; }

        void Run(CancellationToken cancellationToken);
    }
}
