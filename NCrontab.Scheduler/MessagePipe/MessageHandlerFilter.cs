using System;

namespace NCrontab.Scheduler.MessagePipe
{
    internal sealed class PredicateFilter<T> : MessageHandlerFilter<T>
    {
        readonly Func<T, bool> predicate;

        public PredicateFilter(Func<T, bool> predicate)
        {
            this.predicate = predicate;
            this.Order = int.MinValue; // predicate filter first.
        }

        public override void Handle(T message, Action<T> next)
        {
            if (this.predicate(message))
            {
                next(message);
            }
        }
    }

    public abstract class MessageHandlerFilter<TMessage> : IMessageHandlerFilter
    {
        public abstract void Handle(TMessage message, Action<TMessage> next);
    }
}