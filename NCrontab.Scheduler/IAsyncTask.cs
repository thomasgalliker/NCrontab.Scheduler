using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public interface IAsyncTask
    {
        string CronExpression { get; }

        Task RunAsync(CancellationToken cancellationToken);
    }
}
