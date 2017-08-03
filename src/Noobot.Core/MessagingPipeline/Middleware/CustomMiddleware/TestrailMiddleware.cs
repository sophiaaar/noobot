
using System;
using System.Collections.Generic;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using Gurock.TestRail;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Noobot.Core.MessagingPipeline.Middleware.CustomMiddleware
{
    internal class TestrailMiddleware : MiddlewareBase
    {
        TestrailParsing _parse = new TestrailParsing();

        public TestrailMiddleware(IMiddleware next) : base(next)
        {
            HandlerMappings = new[]
            {
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("suites"),
                    },
                    Description = "Lists all suites within the a Testrail project eg suites 2",
                    EvaluatorFunc = SuitesHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("suite_id"),
                    },
                    Description = "Gets the suite if you know the id. eg suite_id 45",
                    EvaluatorFunc = SuiteIDHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("suite_search"),
                    },
                    Description = "Lists suites within a project containing a search term. eg suite_search 1 2D",
                    EvaluatorFunc = SuiteSearchHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new ExactMatchHandle("projects"),
                    },
                    Description = "Lists all projects on Testrail",
                    EvaluatorFunc = ListProjectsHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("sections"),
                    },
                    Description = "Lists all sections within the Unity Testrail project given a project ID and suite ID eg sections 1 2",
                    EvaluatorFunc = SectionsHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("plans"),
                    },
                    Description = "Lists all plans within a Testrail project given a project ID eg plans 2",
                    EvaluatorFunc = PlansHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("runs"),
                    },
                    Description = "Lists all runs (that are not a part of a plan) within a Testrail project given a project ID eg runs 2",
                    EvaluatorFunc = RunsHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("tests"),
                    },
                    Description = "Lists all tests for a test run eg tests 2",
                    EvaluatorFunc = TestsHandler
                }
            };
        }

        private APIClient ConnectToTestrail()
        {
            APIClient client = new APIClient("http://qatestrail.hq.unity3d.com");
            client.User = ""; //TODO - make this able to log in via slack?
            client.Password = ""; //store this in a config file sophiadebug
            return client;
        }

        private IEnumerable<ResponseMessage> SuitesHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("suites".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! suites [project_id] eg suites 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                string responseFromAPI = "";
                List<Attachment> suiteAttachments = new List<Attachment>();
                List<List<Attachment>> listOfLists = new List<List<Attachment>>();

                try
                {
                    JArray c = (JArray)client.SendGet($"get_suites/{searchTerm}");
                    JArray parsed = _parse.ParseSuites(c);
                    suiteAttachments = _parse.CreateAttachmentsFromSuites(parsed);
                    responseFromAPI = "";
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString(); // prettify these later
                }
                if (suiteAttachments.Count != 0)
                {
                    if (suiteAttachments.Count > 15)
                    {
                        listOfLists = _parse.SplitList(suiteAttachments, 14);
                        foreach (List<Attachment> list in listOfLists)
                        {
                            yield return message.ReplyToChannel(responseFromAPI, list);
                        }
                        yield return message.ReplyToChannel("Pin whatever you need for future use!");
                        //yield return message.ReplyToChannel(responseFromAPI, suiteAttachments);
                    }
                    else
                    {
                        yield return message.ReplyToChannel(responseFromAPI, suiteAttachments);
                        yield return message.ReplyToChannel("Pin whatever you need for future use!");
                    }
                }
                else
                {
                    yield return message.ReplyToChannel(responseFromAPI);
                }
            }
        }

        private IEnumerable<ResponseMessage> SuiteIDHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("suite_id".Length).Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! suite_id [suite_id] eg suite_id 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                List<Attachment> suiteAttachments = new List<Attachment>();
                string responseFromAPI = "";

                try
                {
                    JObject c = (JObject)client.SendGet($"get_suite/{searchTerm}");
                    JObject parsed = _parse.ParseSuiteID(c);
                    suiteAttachments = _parse.CreateAttachmentsFromSuiteID(parsed);
                    responseFromAPI = "";
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }
                yield return message.ReplyToChannel(responseFromAPI, suiteAttachments);
            }
        }

        private IEnumerable<ResponseMessage> SuiteSearchHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("suite_search".Length).Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! eg suite_search 1 animation");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                List<Attachment> suiteAttachments = new List<Attachment>();
                string responseFromAPI = "";

                string[] terms = searchTerm.Split(' ');

                try
                {
                    //parse first
                    JArray c = (JArray)client.SendGet($"get_suites/{terms[0]}");
                    JArray parsed = _parse.ParseSuites(c);
                    suiteAttachments = _parse.CreateAttachmentsFromSuiteSearch(parsed, terms[1]);
                    responseFromAPI = $"Here are the suites in project {terms[0]} containing the term \"{terms[1]}\": ";
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }
                yield return message.ReplyToChannel(responseFromAPI, suiteAttachments);
            }
        }

        private IEnumerable<ResponseMessage> ListProjectsHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            yield return message.IndicateTypingOnChannel();
            APIClient client = ConnectToTestrail();
            List<Attachment> projectAttachments = new List<Attachment>();
            string responseFromAPI = "";

            try
            { 
                JArray c = (JArray)client.SendGet($"get_projects");
                JArray parsed = _parse.ParseProjects(c);
                projectAttachments = _parse.CreateAttachmentsFromProjects(parsed);
                responseFromAPI = "";
            }
            catch (APIException e)
            {
                responseFromAPI = e.ToString();
            }
            yield return message.ReplyToChannel(responseFromAPI, projectAttachments);
        }

        private IEnumerable<ResponseMessage> SectionsHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("sections".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! Needs to be a suite_id within the Unity project. sections [project_id] [suite_id] eg sections 1 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                string responseFromAPI = "";
                List<Attachment> listOfSectionAttachments = new List<Attachment>();

                string[] terms = searchTerm.Split(' ');

                try
                {
                    JArray c = (JArray)client.SendGet($"get_sections/{terms[0]}&suite_id={terms[1]}"); //need to get IDs first
                    JArray parsed = _parse.ParseSections(c);
                    listOfSectionAttachments = _parse.CreateAttachmentsFromSections(c);
                    responseFromAPI = "";
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }
                yield return message.ReplyToChannel(responseFromAPI, listOfSectionAttachments);
            }
        }

        private IEnumerable<ResponseMessage> PlansHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("plans".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! plans [project_id] eg plans 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                List<Attachment> plansAttachments = new List<Attachment>();
                List<List<Attachment>> listOfLists = new List<List<Attachment>>();
                string responseFromAPI = "";

                try
                {
                    JArray c = (JArray)client.SendGet($"get_plans/{searchTerm}");
                    JArray parsed = _parse.ParsePlans(c);
                    plansAttachments = _parse.CreateAttachmentsFromPlans(parsed);
                    responseFromAPI = "";
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }

                if (plansAttachments.Count != 0)
                {
                    if (plansAttachments.Count > 15)
                    {
                        listOfLists = _parse.SplitList(plansAttachments, 14);
                        foreach (List<Attachment> list in listOfLists)
                        {
                            yield return message.ReplyToChannel(responseFromAPI, list);
                        }
                        yield return message.ReplyToChannel(responseFromAPI, plansAttachments);
                    }
                    else
                    {
                        yield return message.ReplyToChannel(responseFromAPI, plansAttachments);
                    }
                }
                else
                {
                    yield return message.ReplyToChannel(responseFromAPI);
                }
                yield return message.ReplyToChannel(responseFromAPI);
            }
        }

        private IEnumerable<ResponseMessage> RunsHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("runs".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! runs [project_id] eg runs 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                List<Attachment> runsAttachments = new List<Attachment>();
                List<List<Attachment>> listOfLists = new List<List<Attachment>>();
                string responseFromAPI = "";

                try
                {
                    JArray c = (JArray)client.SendGet($"get_runs/{searchTerm}");
                    JArray parsed = _parse.ParseRuns(c);
                    runsAttachments = _parse.CreateAttachmentsFromRuns(parsed);
                    responseFromAPI = "";
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }

                if (runsAttachments.Count != 0)
                {
                    if (runsAttachments.Count > 10)
                    {
                        listOfLists = _parse.SplitList(runsAttachments, 9);
                        foreach (List<Attachment> list in listOfLists)
                        {
                            yield return message.ReplyToChannel(responseFromAPI, list);
                        }
                        yield return message.ReplyToChannel(responseFromAPI, runsAttachments);
                    }
                    else
                    {
                        yield return message.ReplyToChannel(responseFromAPI, runsAttachments);
                    }
                }
                else
                {
                    yield return message.ReplyToChannel(responseFromAPI);
                }
                yield return message.ReplyToChannel(responseFromAPI);
            }
        }

        private IEnumerable<ResponseMessage> TestsHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("tests".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! tests [test_id] eg tests 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                string responseFromAPI = "";

                try
                {
                    JArray c = (JArray)client.SendGet($"get_tests/{searchTerm}");
                    responseFromAPI = c.ToString();
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }
                yield return message.ReplyToChannel(responseFromAPI);
            }
        }
    }
}