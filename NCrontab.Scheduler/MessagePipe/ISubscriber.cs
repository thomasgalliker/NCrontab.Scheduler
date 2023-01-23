using System;

namespace NCrontab.Scheduler.MessagePipe
{
    public interface ISubscriber<T>
    {
        IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters);
    }
}
