using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public interface IAsyncScheduledTask : ITask
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
