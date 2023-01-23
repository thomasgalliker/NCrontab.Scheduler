using System;

namespace NCrontab.Scheduler.MessagePipe
{
    public static class SubscriberExtensions
    {
        public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscriber, Action<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters)
        {
            return subscriber.Subscribe(new AnonymousMessageHandler<TMessage>(handler), filters);
        }
    }
}