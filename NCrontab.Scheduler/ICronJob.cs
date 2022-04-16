using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler
{
    public interface ICronJob
    {
        Task RunJob(CancellationToken cancellationToken);
    }
}
