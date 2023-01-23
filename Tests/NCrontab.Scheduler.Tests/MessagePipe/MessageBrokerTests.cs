using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NCrontab.Scheduler.MessagePipe;
using Xunit;

namespace NCrontab.Scheduler.Tests.MessagePipe
{
    internal class MessageBrokerTests
    {
        [Fact]
        public void ShouldDoSomething_WhenCondition_ExpectedResult()
        {
            // Arrange
            var messageBroker = new MessageBroker<Guid>();
            messageBroker.Subscribe(new MessageHandlerClient());
            messageBroker.Subscribe()

            // Act


            // Assert

        }

        private class MessageHandlerClient : IMessageHandler<Guid>
        {
            public void Handle(Guid message)
            {
                throw new NotImplementedException();
            }
        }
    }
}
