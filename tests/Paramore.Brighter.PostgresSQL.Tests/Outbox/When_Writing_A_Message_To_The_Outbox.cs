#region Licence

/* The MIT License (MIT)
Copyright © 2014 Francesco Pighi <francesco.pighi@gmail.com>

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
using Paramore.Brighter.Outbox.PostgreSql;
using Xunit;

namespace Paramore.Brighter.PostgresSQL.Tests.Outbox
{
    [Trait("Category", "PostgreSql")]
    [Collection("PostgreSql OutBox")]
    public class SqlOutboxWritingMessageTests : IDisposable
    {
        private readonly string _key1 = "name1";
        private readonly string _key2 = "name2";
        private readonly Message _messageEarliest;
        private readonly PostgreSqlOutbox _sqlOutbox;
        private Message _storedMessage;
        private readonly string _value1 = "value1";
        private readonly string _value2 = "value2";
        private readonly PostgreSqlTestHelper _PostgreSqlTestHelper;

        public SqlOutboxWritingMessageTests()
        {
            _PostgreSqlTestHelper = new PostgreSqlTestHelper();
            _PostgreSqlTestHelper.SetupMessageDb();

            _sqlOutbox = new PostgreSqlOutbox(_PostgreSqlTestHelper.OutboxConfiguration);
            var messageHeader = new MessageHeader(Guid.NewGuid(), "test_topic", MessageType.MT_DOCUMENT, DateTime.UtcNow.AddDays(-1), 5, 5);
            messageHeader.Bag.Add(_key1, _value1);
            messageHeader.Bag.Add(_key2, _value2);

            _messageEarliest = new Message(messageHeader, new MessageBody("message body"));
            _sqlOutbox.Add(_messageEarliest);
        }

        [Fact]
        public void When_Writing_A_Message_To_The_PostgreSql_Outbox()
        {
            _storedMessage = _sqlOutbox.Get(_messageEarliest.Id);

            //_should_read_the_message_from_the__sql_outbox
            _storedMessage.Body.Value.Should().Be(_messageEarliest.Body.Value);
            //_should_read_the_message_header_first_bag_item_from_the__sql_outbox
            _storedMessage.Header.Bag.ContainsKey(_key1).Should().BeTrue();
            _storedMessage.Header.Bag[_key1].Should().Be(_value1);
            //_should_read_the_message_header_second_bag_item_from_the__sql_outbox
            _storedMessage.Header.Bag.ContainsKey(_key2).Should().BeTrue();
            _storedMessage.Header.Bag[_key2].Should().Be(_value2);
            //_should_read_the_message_header_timestamp_from_the__sql_outbox
            _storedMessage.Header.TimeStamp.Should().Be(_messageEarliest.Header.TimeStamp);
            //_should_read_the_message_header_topic_from_the__sql_outbox
            _storedMessage.Header.Topic.Should().Be(_messageEarliest.Header.Topic);
            //_should_read_the_message_header_type_from_the__sql_outbox
            _storedMessage.Header.MessageType.Should().Be(_messageEarliest.Header.MessageType);
        }

        public void Dispose()
        {
            _PostgreSqlTestHelper.CleanUpTable();
        }
    }
}
