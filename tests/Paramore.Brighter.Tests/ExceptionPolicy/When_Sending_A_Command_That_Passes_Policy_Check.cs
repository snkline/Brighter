﻿#region Licence
/* The MIT License (MIT)
Copyright � 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the �Software�), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED �AS IS�, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;
using FluentAssertions;
using Xunit;
using Paramore.Brighter.Policies.Handlers;
using Paramore.Brighter.Tests.ExceptionPolicy.TestDoubles;
using Paramore.Brighter.Tests.TestDoubles;
using Polly;
using TinyIoC;

namespace Paramore.Brighter.Tests.ExceptionPolicy
{
    public class CommandProcessorWithExceptionPolicyNothingThrowTests
    {
        private CommandProcessor _commandProcessor;
        private readonly MyCommand _myCommand = new MyCommand();
        private int _retryCount;

        public CommandProcessorWithExceptionPolicyNothingThrowTests()
        {
            var registry = new SubscriberRegistry();
            registry.Register<MyCommand, MyDoesNotFailPolicyHandler>();

            var container = new TinyIoCContainer();
            var handlerFactory = new TinyIocHandlerFactory(container);
            container.Register<IHandleRequests<MyCommand>, MyDoesNotFailPolicyHandler>("MyDoesNotFailPolicyHandler");
            container.Register<IHandleRequests<MyCommand>, ExceptionPolicyHandler<MyCommand>>("MyExceptionPolicyHandler");

            var policyRegistry = new PolicyRegistry();

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetry(new[]
                {
                    1.Seconds(),
                    2.Seconds(),
                    3.Seconds()
                }, (exception, timeSpan) =>
                {
                    _retryCount++;
                });
            policyRegistry.Add("MyDivideByZeroPolicy", policy);

            MyDoesNotFailPolicyHandler.ReceivedCommand = false;

            _commandProcessor = new CommandProcessor(registry, handlerFactory, new InMemoryRequestContextFactory(), policyRegistry);
        }

        //We have to catch the final exception that bubbles out after retry
        [Fact]
        public void When_Sending_A_Command_That_Passes_Policy_Check()
        {
            _commandProcessor.Send(_myCommand);

           //_should_send_the_command_to_the_command_handler
            MyDoesNotFailPolicyHandler.Shouldreceive(_myCommand).Should().BeTrue();
            //_should_not_retry
            _retryCount.Should().Be(0);
        }
    }
}