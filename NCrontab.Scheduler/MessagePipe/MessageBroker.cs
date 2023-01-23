using System;
using System.Runtime.CompilerServices;
using NCrontab.Scheduler.Internals;

namespace NCrontab.Scheduler.MessagePipe
{
    [Preserve]
    public sealed class MessageBroker<TMessage> : IDisposable
    {
        private readonly FreeList<(IMessageHandler<TMessage>, MessageHandlerFilter<TMessage>)> handlers;
        private readonly object lockObj = new object();
        private bool isDisposed;

        public MessageBroker()
        {
            this.handlers = new FreeList<(IMessageHandler<TMessage>, MessageHandlerFilter<TMessage>)>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Publish(TMessage message)
        {
            var messageHandlers = this.handlers.GetValues();
            for (var i = 0; i < messageHandlers.Length; i++)
            {
                var m = messageHandlers[i];
                var ok = m.Item2.Handle(message)
                messageHandlers[i].Item1.Handle(message);
            }
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<ScheduledEventArgs>[] filters)
        {
            lock (this.lockObj)
            {
                var subscriptionKey = this.handlers.Add((handler, filters));
                var subscription = new Subscription(this, subscriptionKey);
                return subscription;
            }
        }

        public void Dispose()
        {
            lock (this.lockObj)
            {
                // Dispose is called when scope is finished.
                if (!this.isDisposed && this.handlers.TryDispose(out _))
                {
                    this.isDisposed = true;
                }
            }
        }

        private sealed class Subscription : IDisposable
        {
            private bool isDisposed;
            private readonly MessageBroker<TMessage> messageBroker;
            private readonly int subscriptionKey;

            public Subscription(MessageBroker<TMessage> messageBroker, int subscriptionKey)
            {
                this.messageBroker = messageBroker;
                this.subscriptionKey = subscriptionKey;
            }

            public void Dispose()
            {
                if (!this.isDisposed)
                {
                    this.isDisposed = true;
                    lock (this.messageBroker.lockObj)
                    {
                        if (!this.messageBroker.isDisposed)
                        {
                            this.messageBroker.handlers.Remove(this.subscriptionKey, true);
                        }
                    }
                }
            }
        }
    }
}