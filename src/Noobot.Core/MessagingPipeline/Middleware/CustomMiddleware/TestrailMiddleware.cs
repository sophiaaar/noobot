
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
            //yield return message.ReplyDirectlyToUser("sophiadebug search term " + searchTerm);

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
                    JArray parsed = ParseSuites(c);
                    suiteAttachments = CreateAttachmentsFromSuites(parsed);
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
                        listOfLists = splitList(suiteAttachments, 14);
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
                    JObject parsed = ParseSuiteID(c);
                    suiteAttachments = CreateAttachmentsFromSuiteID(parsed);
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
                    JArray parsed = ParseSuites(c);
                    suiteAttachments = CreateAttachmentsFromSuiteSearch(parsed, terms[1]);
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
                JArray parsed = ParseProjects(c);
                projectAttachments = CreateAttachmentsFromProjects(parsed);
                responseFromAPI = "";
                //responseFromAPI = parsed + "\n I suggest pinning that message so you don't need to request it again!";
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
            //yield return message.ReplyDirectlyToUser("sophiadebug search term " + searchTerm);

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
                    JArray parsed = ParseSections(c);
                    listOfSectionAttachments = CreateAttachmentsFromSections(c);
                    //responseFromAPI = parsed.ToString();
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
            //yield return message.ReplyDirectlyToUser("sophiadebug search term " + searchTerm);

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! plans [project_id] eg plans 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                string responseFromAPI = "";

                try
                {
                    JArray c = (JArray)client.SendGet($"get_plans/{searchTerm}");
                    string parsed = ParsePlans(c);
                    responseFromAPI = parsed;
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }
                yield return message.ReplyDirectlyToUser(responseFromAPI);
            }
        }

        private IEnumerable<ResponseMessage> RunsHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("runs".Length).Trim();
            //yield return message.ReplyDirectlyToUser("sophiadebug search term " + searchTerm);

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel("Give me something to search! runs [project_id] eg runs 1");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                APIClient client = ConnectToTestrail();
                string responseFromAPI = "";

                try
                {
                    JArray c = (JArray)client.SendGet($"get_runs/{searchTerm}");
                    responseFromAPI = c.ToString();
                }
                catch (APIException e)
                {
                    responseFromAPI = e.ToString();
                }
                yield return message.ReplyDirectlyToUser(responseFromAPI);
            }
        }

        private IEnumerable<ResponseMessage> TestsHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring("tests".Length).Trim();
            //yield return message.ReplyDirectlyToUser("sophiadebug search term " + searchTerm);

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
                yield return message.ReplyDirectlyToUser(responseFromAPI);
            }
        }

        private JArray ParseSections(JArray array)
        {
            foreach (JObject arrayObject in array)
            {
                arrayObject.Property("suite_id").Remove();
                arrayObject.Property("parent_id").Remove();
                arrayObject.Property("display_order").Remove();
                arrayObject.Property("depth").Remove();
            }            
            return array;
        }

        private JArray ParseSuites(JArray array)
        {
            foreach (JObject arrayObject in array)
            {
                arrayObject.Property("project_id").Remove();
                arrayObject.Property("is_master").Remove();
                arrayObject.Property("is_baseline").Remove();
                arrayObject.Property("is_completed").Remove();
                arrayObject.Property("completed_on").Remove();
            }
            return array;
        }

        private JObject ParseSuiteID(JObject jObj)
        {
            jObj.Property("id").Remove();
            jObj.Property("project_id").Remove();
            jObj.Property("is_master").Remove();
            jObj.Property("is_baseline").Remove();
            jObj.Property("is_completed").Remove();
            jObj.Property("completed_on").Remove();
            return jObj;
        }

        private JArray ParseProjects(JArray array)
        {
            foreach (JObject arrayObject in array)
            {
                arrayObject.Property("show_announcement").Remove();
                arrayObject.Property("announcement").Remove();
                arrayObject.Property("is_completed").Remove();
                arrayObject.Property("completed_on").Remove();
                arrayObject.Property("suite_mode").Remove();
            }
            return array;
        }

        private string ParsePlans(JArray array)
        {
            foreach (JObject arrayObject in array)
            {
                arrayObject.Property("assignedto_id").Remove();
                arrayObject.Property("is_completed").Remove();
                arrayObject.Property("completed_on").Remove();
                arrayObject.Property("blocked_count").Remove();
                arrayObject.Property("retest_count").Remove();
                arrayObject.Property("custom_status1_count").Remove();
                arrayObject.Property("custom_status2_count").Remove();
                arrayObject.Property("custom_status3_count").Remove();
                arrayObject.Property("custom_status4_count").Remove();
                arrayObject.Property("custom_status5_count").Remove();
                arrayObject.Property("custom_status6_count").Remove();
                arrayObject.Property("custom_status7_count").Remove();
                arrayObject.Property("created_on").Remove();
                arrayObject.Property("created_by").Remove();
            }
            return array.ToString();
        }

        private List<Attachment> CreateAttachmentsFromSuites(JArray array)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                attach.Title = jObj.Property("name").Value.ToString();
                attach.TitleLink = jObj.Property("url").Value.ToString();
                attach.Text = "ID = " + jObj.Property("id").Value.ToString() + "\n" + jObj.Property("description").Value.ToString();
                attachments.Add(attach);
            }
            return attachments;
        }

        private List<Attachment> CreateAttachmentsFromSuiteSearch(JArray array, string searchTerm)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                if (jObj.Property("name").Value.ToString().ToLower().Contains(searchTerm.ToLower()))
                {
                    attach.Title = jObj.Property("name").Value.ToString();
                    attach.TitleLink = jObj.Property("url").Value.ToString();
                    attach.Text = "ID = " + jObj.Property("id").Value.ToString() + "\n" + jObj.Property("description").Value.ToString();
                    attachments.Add(attach);
                }
            }
            return attachments;
        }

        private List<Attachment> CreateAttachmentsFromSuiteID(JObject jObj)
        {
            List<Attachment> attachments = new List<Attachment>();
            Attachment attach = new Attachment();
            attach.Title = jObj.Property("name").Value.ToString();
            attach.TitleLink = jObj.Property("url").Value.ToString();
            if (!string.IsNullOrEmpty(jObj.Property("description").Value.ToString()))
            {
                attach.Text = jObj.Property("description").Value.ToString();
            }
            else
            {
                attach.Text = "Description = null";
            }
            attachments.Add(attach);
            return attachments;
        }

        private List<Attachment> CreateAttachmentsFromProjects(JArray array)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                attach.Title = jObj.Property("name").Value.ToString();
                attach.TitleLink = jObj.Property("url").Value.ToString();
                attach.Text = "ID = " + jObj.Property("id").Value.ToString();
                attachments.Add(attach);
            }
            return attachments;
        }

        private List<Attachment> CreateAttachmentsFromSections(JArray array)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                attach.Title = jObj.Property("name").Value.ToString();
                //attach.TitleLink = jObj.Property("url").Value.ToString();
                attach.Text = "ID = " + jObj.Property("id").Value.ToString() + "\n" + jObj.Property("description").Value.ToString();
                attachments.Add(attach);
            }
            return attachments;
        }

        private List<List<Attachment>> splitList(List<Attachment> listOfAttachments, int nSize)
        {
            var list = new List<List<Attachment>>();

            for (int i=0; i<listOfAttachments.Count; i += nSize)
            {
                list.Add(listOfAttachments.GetRange(i, Math.Min(nSize, listOfAttachments.Count - i)));
            }
            return list;
        }
    }
}