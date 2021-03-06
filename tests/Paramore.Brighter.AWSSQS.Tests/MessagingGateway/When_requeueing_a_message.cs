using System;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using FluentAssertions;
using Newtonsoft.Json;
using Paramore.Brighter.AWSSQS.Tests.TestDoubles;
using Paramore.Brighter.MessagingGateway.AWSSQS;
using Xunit;

namespace Paramore.Brighter.AWSSQS.Tests.MessagingGateway
{
    [Collection("AWS")]
    [Trait("Category", "AWS")]
    public class SqsMessageProducerRequeueTests : IDisposable
    {
        private readonly IAmAMessageProducer _sender;
        private Message _requeuedMessage;
        private Message _receivedMessage;
        private readonly IAmAChannel _channel;
        private readonly ChannelFactory _channelFactory;
        private readonly Message _message;
        private readonly MyCommand _myCommand;
        private readonly Guid _correlationId;
        private readonly string _replyTo;
        private readonly string _contentType;
        private readonly string _topicName;
        private Connection<MyCommand> _connection; 

        public SqsMessageProducerRequeueTests()
        {
            _myCommand = new MyCommand{Value = "Test"};
            _correlationId = Guid.NewGuid();
            _replyTo = "http:\\queueUrl";
            _contentType = "text\\plain";
            var channelName = $"Producer-Requeue-Tests-{Guid.NewGuid().ToString()}".Truncate(45);
            _topicName = $"Producer-Requeue-Tests-{Guid.NewGuid().ToString()}".Truncate(45);
            _connection = new Connection<MyCommand>(
                name: new ConnectionName(channelName),
                channelName: new ChannelName(channelName),
                routingKey: new RoutingKey(_topicName)
                );
            
            _message = new Message(
                new MessageHeader(_myCommand.Id, _topicName, MessageType.MT_COMMAND, _correlationId, _replyTo, _contentType),
                new MessageBody(JsonConvert.SerializeObject((object) _myCommand))
            );
 
            //Must have credentials stored in the SDK Credentials store or shared credentials file
            var credentialChain = new CredentialProfileStoreChain();
            
            (AWSCredentials credentials, RegionEndpoint region) = CredentialsChain.GetAwsCredentials();
            var awsConnection = new AWSMessagingGatewayConnection(credentials, region);
            _sender = new SqsMessageProducer(awsConnection);
            _channelFactory = new ChannelFactory(awsConnection, new SqsMessageConsumerFactory(awsConnection));
            _channel = _channelFactory.CreateChannel(_connection);
        }

        [Fact]
        public void When_requeueing_a_message()
        {
            _sender.Send(_message);
            _receivedMessage = _channel.Receive(2000); 
            _channel.Requeue(_receivedMessage);

            _requeuedMessage = _channel.Receive(1000);
            
            //clear the queue
            _channel.Acknowledge(_requeuedMessage );

            _requeuedMessage.Body.Value.Should().Be(_receivedMessage.Body.Value);
        }

        public void Dispose()
        {
            _channelFactory.DeleteTopic();
            _channelFactory.DeleteQueue();
        }
    }
}
