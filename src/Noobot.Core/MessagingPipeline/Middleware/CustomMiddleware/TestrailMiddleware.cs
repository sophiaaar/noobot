
using System;
using System.Collections.Generic;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using Noobot.Core.Configuration;
using Gurock.TestRail;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Noobot.Core.MessagingPipeline.Middleware.CustomMiddleware
{
    internal class TestrailMiddleware : MiddlewareBase
    {
        TestrailParsing _parse = new TestrailParsing();
        private readonly IConfigReader _configReader = new ConfigReader();
               

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
                        new StartsWithHandle("test_runs"),
                    },
                    Description = "Lists all runs (that are not a part of a plan) within a Testrail project given a project ID eg test_runs 2",
                    EvaluatorFunc = RunsHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("run_report"),
                    },
                    Description = "Details a run of a given run ID eg run_report 2",
                    EvaluatorFunc = RunHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("run_search"),
                    },
                    Description = "Lists runs within a project given a search term eg run_search 1 RAT",
                    EvaluatorFunc = RunSearchHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("run_today"),
                    },
                    Description = "Lists runs within a project given a search term that were completed today eg run_today 1 RAT",
                    EvaluatorFunc = RunTodayHandler
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[]
                    {
                        new StartsWithHandle("close_run"),
                    },
                    Description = "Closes the run and displays result. eg close_run 1",
                    EvaluatorFunc = CloseRunHandler
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
            client.User = _configReader.TestRailUser;
            client.Password = _configReader.TestRailPass;
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

                string[] terms = searchTerm.Split(new char[] { ' ' }, 2);
                if (terms.Length == 1)
                {
                    yield return message.ReplyToChannel("Searching requires a project id and search term! eg suite_search 1 animation");
                }
                else
                {
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
                yield return message.ReplyToChannel("Give me something to search! eg sections [project_id] [suite_id] eg sections 1 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                string responseFromAPI = "";
                List<Attachment> sectionAttachments = new List<Attachment>();
                List<List<Attachment>> listOfLists = new List<List<Attachment>>();

                string[] terms = searchTerm.Split(new char[] { ' ' }, 2);
                if (terms.Length == 1)
                {
                    yield return message.ReplyToChannel("Search using a project id and a suite id. eg sections [project_id] [suite_id] eg sections 1 1");
                }
                else
                {

                    try
                    {
                        JArray c = (JArray)client.SendGet($"get_sections/{terms[0]}&suite_id={terms[1]}"); //need to get IDs first
                        JArray parsed = _parse.ParseSections(c);
                        sectionAttachments = _parse.CreateAttachmentsFromSections(parsed);
                        responseFromAPI = "";
                    }
                    catch (APIException e)
                    {
                        responseFromAPI = e.ToString();
                    }
                    if (sectionAttachments.Count != 0)
                    {
                        if (sectionAttachments.Count > 10)
                        {
                            listOfLists = _parse.SplitList(sectionAttachments, 9);
                            foreach (List<Attachment> list in listOfLists)
                            {
                                yield return message.ReplyToChannel(responseFromAPI, list);
                            }
                            yield return message.ReplyToChannel(responseFromAPI, sectionAttachments);
                        }
                        else
                        {
                            yield return message.ReplyToChannel(responseFromAPI, sectionAttachments);
                        }
                    }
                    else
                    {
                        yield return message.ReplyToChannel(responseFromAPI);
                    }
                    //yield return message.ReplyToChannel(responseFromAPI);
                }
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

        private IEnumerable<ResponseMessage> RunHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("run_report".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! run_report [run_id] eg run_report 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                List<Attachment> runAttachments = new List<Attachment>();
                string responseFromAPI = "";

                try
                {
                    JObject c = (JObject)client.SendGet($"get_run/{searchTerm}");
                    JObject parsed = _parse.ParseRun(c);
                    runAttachments = _parse.CreateAttachmentsFromRun(parsed);
                    responseFromAPI = "";
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }
                yield return message.ReplyToChannel(responseFromAPI, runAttachments);
            }
        }

        private IEnumerable<ResponseMessage> RunSearchHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("run_search".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! run_search [project_id] [search term] eg run_search 1 RAT");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                List<Attachment> runSearchAttachments = new List<Attachment>();
                List<List<Attachment>> listOfLists = new List<List<Attachment>>();
                string responseFromAPI = "";

                string[] terms = searchTerm.Split(new char[] { ' ' }, 2);
                if (terms.Length == 1)
                {
                    yield return message.ReplyToChannel("Searching requires a project id and search term! eg run_search 1 animation");
                }
                else
                {
                    try
                    {
                        //parse first
                        JArray c = (JArray)client.SendGet($"get_runs/{terms[0]}");
                        JArray parsed = _parse.ParseRuns(c);
                        runSearchAttachments = _parse.CreateAttachmentsFromRunSearch(parsed, terms[1]);
                        responseFromAPI = $"Here are the runs in project {terms[0]} containing the term \"{terms[1]}\": ";
                    }
                    catch (APIException e)
                    {
                        responseFromAPI = e.ToString();
                    }
                    //yield return message.ReplyToChannel(responseFromAPI, runSearchAttachments);
                    if (runSearchAttachments.Count != 0)
                    {
                        if (runSearchAttachments.Count > 6)
                        {
                            listOfLists = _parse.SplitList(runSearchAttachments, 5);
                            foreach (List<Attachment> list in listOfLists)
                            {
                                yield return message.ReplyToChannel(responseFromAPI, list);
                            }
                            yield return message.ReplyToChannel(responseFromAPI, runSearchAttachments);
                        }
                        else
                        {
                            yield return message.ReplyToChannel(responseFromAPI, runSearchAttachments);
                        }
                    }
                    else
                    {
                        yield return message.ReplyToChannel(responseFromAPI);
                    }
                    //yield return message.ReplyToChannel(responseFromAPI);
                }
            }
        }

        private IEnumerable<ResponseMessage> RunTodayHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("run_today".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! run_today [project_id] [search term] eg run_today 1 RAT");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                List<Attachment> runTodayAttachments = new List<Attachment>();
                List<List<Attachment>> listOfLists = new List<List<Attachment>>();
                string responseFromAPI = "";

                string[] terms = searchTerm.Split(new char[] { ' ' }, 2);
                if (terms.Length == 1)
                {
                    yield return message.ReplyToChannel("Searching requires a project id and search term! eg run_today 1 animation");
                }
                else
                {
                    try
                    {
                        //parse first
                        JArray c = (JArray)client.SendGet($"get_runs/{terms[0]}");
                        JArray parsed = _parse.ParseRuns(c);
                        runTodayAttachments = _parse.CreateAttachmentsFromRunToday(parsed, terms[1]);
                        responseFromAPI = $"Here are the runs from today in project {terms[0]} containing the term \"{terms[1]}\": ";
                    }
                    catch (APIException e)
                    {
                        responseFromAPI = e.ToString();
                    }
                    //yield return message.ReplyToChannel(responseFromAPI, runSearchAttachments);
                    if (runTodayAttachments.Count != 0)
                    {
                        if (runTodayAttachments.Count > 6)
                        {
                            listOfLists = _parse.SplitList(runTodayAttachments, 5);
                            foreach (List<Attachment> list in listOfLists)
                            {
                                yield return message.ReplyToChannel(responseFromAPI, list);
                            }
                            yield return message.ReplyToChannel(responseFromAPI, runTodayAttachments);
                        }
                        else
                        {
                            yield return message.ReplyToChannel(responseFromAPI, runTodayAttachments);
                        }
                    }
                    else
                    {
                        yield return message.ReplyToChannel(responseFromAPI);
                    }
                    //yield return message.ReplyToChannel(responseFromAPI);
                }
            }
        }

        private IEnumerable<ResponseMessage> RunsHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("test_runs".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! runs [project_id] eg test_runs 1");
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

        private IEnumerable<ResponseMessage> CloseRunHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("close_run".Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! close_run [run_id] eg close_run 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                List<Attachment> runAttachments = new List<Attachment>();
                string responseFromAPI = "";

                try
                {
                    JObject jObjRun = (JObject)client.SendPost($"close_run/{searchTerm}", null);
                    JObject parsedRun = _parse.ParseRun(jObjRun);

					string project_id = parsedRun.Property("project_id").Value.ToString();
					string suite_id = parsedRun.Property("suite_id").Value.ToString();
					JArray jArrSections = (JArray)client.SendGet($"get_sections/{project_id}&suite_id={suite_id}");
                    JArray parsedSections = _parse.ParseSectionGetName(jArrSections);

                    runAttachments = _parse.CreateAttachmentsFromCloseRun(parsedRun, parsedSections);

					//JArray c = (JArray)client.SendGet($"get_sections/{terms[0]}&suite_id={terms[1]}"); //need to get IDs first
					//JArray parsed = _parse.ParseSections(c);
					//sectionAttachments = _parse.CreateAttachmentsFromSections(c);

                    responseFromAPI = "";
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }
                yield return message.ReplyToChannel(responseFromAPI, runAttachments);
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