using System;
using System.Collections.Generic;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;

namespace Noobot.Core.MessagingPipeline.Middleware.StandardMiddleware
{
    internal class AboutMiddleware : MiddlewareBase
    {
        public AboutMiddleware(IMiddleware next) : base(next)
        {
            HandlerMappings = new[]
            {
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new ExactMatchHandle("about"), 
                    },
                    Description = "Tells you some stuff about this bot :-)",
                    EvaluatorFunc = AboutHandler
                }
            };
        }

        private IEnumerable<ResponseMessage> AboutHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            yield return message.ReplyToChannel("Noobot - Created by Simon Colmer " + DateTime.Now.Year);
            yield return message.ReplyToChannel("I am an extensible SlackBot built in C# using loads of awesome open source projects.");
            yield return message.ReplyToChannel("Forked by Sophia Clarkke for use with the Testrail API!");
            yield return message.ReplyToChannel("Please find more at http://github.com/noobot/noobot");
        }
    }
}