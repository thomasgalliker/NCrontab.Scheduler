namespace NCrontab.Scheduler.MessagePipe
{
    public interface IMessageHandler<T>
    {
        void Handle(T message);
    }
}