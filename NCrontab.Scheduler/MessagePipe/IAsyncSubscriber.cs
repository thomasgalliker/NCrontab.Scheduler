using System;

namespace NCrontab.Scheduler.MessagePipe
{
    public interface IAsyncSubscriber<T>
    {
        IDisposable SubscribeAsync(IAsyncMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters);
    }
}
