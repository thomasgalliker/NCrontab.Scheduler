using System.Threading.Tasks;

namespace NCrontab.Scheduler.MessagePipe
{
    public interface IAsyncMessageHandler<T>
    {
        Task HandleAsync(T message);
    }
}