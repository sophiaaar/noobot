using System;
using System.Collections.Generic;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;

namespace Noobot.Core.MessagingPipeline.Middleware.StandardMiddleware
{
    internal class HelloMiddleware : MiddlewareBase
    {
        public HelloMiddleware(IMiddleware next) : base(next)
        {
            HandlerMappings = new[]
            {
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new ExactMatchHandle("hello"),
                        new ExactMatchHandle("hi"),
                        new ExactMatchHandle("hey"),
                    },
                    Description = "Says hello! :-)",
                    EvaluatorFunc = HelloHandler
                }
            };
        }

        private IEnumerable<ResponseMessage> HelloHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            yield return message.ReplyToChannel("Hello " + message.Username + "!");
        }
    }
}