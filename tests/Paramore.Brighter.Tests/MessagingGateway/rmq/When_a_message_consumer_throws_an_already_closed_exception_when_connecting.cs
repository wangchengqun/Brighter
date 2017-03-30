#region Licence
/* The MIT License (MIT)
Copyright © 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;
using FluentAssertions;
using Paramore.Brighter.MessagingGateway.RMQ;
using Paramore.Brighter.MessagingGateway.RMQ.MessagingGatewayConfiguration;
using Paramore.Brighter.Tests.MessagingGateway.TestDoubles;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace Paramore.Brighter.Tests.MessagingGateway.RMQ 
{
    [Trait("Category", "RMQ")]
    public class RmqMessageConsumerConnectionClosedTests : IDisposable
    {
        private readonly IAmAMessageProducer _sender;
        private readonly IAmAMessageConsumer _receiver;
        private readonly IAmAMessageConsumer _badReceiver;
        private readonly Message _sentMessage;
        private Exception _firstException;

        public RmqMessageConsumerConnectionClosedTests()
        {
            var messageHeader = new MessageHeader(Guid.NewGuid(), "test2", MessageType.MT_COMMAND);

            messageHeader.UpdateHandledCount();
            _sentMessage = new Message(header: messageHeader, body: new MessageBody("test content"));

            var rmqConnection = new RmqMessagingGatewayConnection
            {
                AmpqUri = new AmqpUriSpecification(new Uri("amqp://guest:guest@localhost:5672/%2f")),
                Exchange = new Exchange("paramore.brighter.exchange")
            };

            _sender = new RmqMessageProducer(rmqConnection);
            _receiver = new RmqMessageConsumer(rmqConnection, _sentMessage.Header.Topic, _sentMessage.Header.Topic, false, 1, false);
            _badReceiver = new AlreadyClosedRmqMessageConsumer(rmqConnection, _sentMessage.Header.Topic, _sentMessage.Header.Topic, false, 1, false);

            _receiver.Purge();
            _sender.Send(_sentMessage);
        }

        [Fact(Skip = "RabbitMQ.Client.Exceptions.OperationInterruptedException : The AMQP operation was interrupted: AMQP close-reason, initiated by Peer, code=503, text=\"COMMAND_INVALID - unknown exchange type 'x-delayed-message'\", classId=40, methodId=10, cause=")]
        public void When_a_message_consumer_throws_an_already_closed_exception_when_connecting()
        {
            _firstException = Catch.Exception(() => _badReceiver.Receive(2000));

            //_should_return_a_channel_failure_exception
            _firstException.Should().BeOfType<ChannelFailureException>();
            //_should_return_an_explainging_inner_exception
            _firstException.InnerException.Should().BeOfType<AlreadyClosedException>();
        }

        public void Dispose()
        {
            _receiver.Purge();
            _sender.Dispose();
            _receiver.Dispose();
        }
    }
}