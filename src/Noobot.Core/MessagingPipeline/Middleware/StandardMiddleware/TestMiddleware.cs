using System;
using System.Collections.Generic;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;

namespace Noobot.Core.MessagingPipeline.Middleware.StandardMiddleware
{
    internal class TestMiddleware : MiddlewareBase
    {
        public TestMiddleware(IMiddleware next) : base(next)
        {
            HandlerMappings = new[]
            {
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new ExactMatchHandle("test"), 
                    },
                    Description = "A test command created by Sophia",
                    EvaluatorFunc = TestHandler
                }
            };
        }

        private IEnumerable<ResponseMessage> TestHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            yield return message.ReplyDirectlyToUser("A test command by Sophia");
            yield return message.ReplyDirectlyToUser("The current time is " + DateTime.Now.ToShortTimeString());
            yield return message.ReplyDirectlyToUser("Please find more at http://github.com/noobot/noobot");
        }
    }
}