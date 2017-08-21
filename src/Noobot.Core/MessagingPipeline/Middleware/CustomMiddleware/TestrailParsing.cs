using System;
using System.Collections.Generic;
using System.Text;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using Gurock.TestRail;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Noobot.Core.MessagingPipeline.Middleware.CustomMiddleware
{
    internal class TestrailParsing
    {
        public JArray ParseSections(JArray array)
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

		public JArray ParseSectionGetName(JArray array)
		{
			foreach (JObject arrayObject in array)
			{
				arrayObject.Property("suite_id").Remove();
				arrayObject.Property("parent_id").Remove();
				arrayObject.Property("display_order").Remove();
				arrayObject.Property("depth").Remove();
                arrayObject.Property("description").Remove();
                arrayObject.Property("id").Remove();
			}
			return array;
		}

        public JArray ParseSuites(JArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                JObject arrayObject = array[i].ToObject<JObject>();
                arrayObject = ParseSuiteID(arrayObject);
            }
            return array;
        }

        public JObject ParseSuiteID(JObject jObj)
        {
            //jObj.Property("id").Remove();
            jObj.Property("project_id").Remove();
            jObj.Property("is_master").Remove();
            jObj.Property("is_baseline").Remove();
            jObj.Property("is_completed").Remove();
            jObj.Property("completed_on").Remove();
            return jObj;
        }

        public JArray ParseProjects(JArray array)
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

        public JObject ParsePlan(JObject jObj)
        {
            jObj.Property("assignedto_id").Remove();
            jObj.Property("is_completed").Remove();
            jObj.Property("completed_on").Remove();
            jObj.Property("blocked_count").Remove();
            jObj.Property("retest_count").Remove();
            jObj.Property("custom_status1_count").Remove();
            jObj.Property("custom_status2_count").Remove();
            jObj.Property("custom_status3_count").Remove();
            jObj.Property("custom_status4_count").Remove();
            jObj.Property("custom_status5_count").Remove();
            jObj.Property("custom_status6_count").Remove();
            jObj.Property("custom_status7_count").Remove();
            jObj.Property("created_on").Remove();
            jObj.Property("created_by").Remove();
            return jObj;
        }

        public JArray ParsePlans(JArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                JObject arrayObject = array[i].ToObject<JObject>();
                arrayObject = ParsePlan(arrayObject);
            }
            return array;
        }

        public JObject ParseRun(JObject jObj)
        {
            jObj.Property("assignedto_id").Remove();
            jObj.Property("config").Remove();
            jObj.Property("config_ids").Remove();
            //jObj.Property("completed_on").Remove();
            jObj.Property("blocked_count").Remove();
            jObj.Property("retest_count").Remove();
            jObj.Property("milestone_id").Remove();
            //jObj.Property("project_id").Remove();
            jObj.Property("include_all").Remove();
            jObj.Property("custom_status1_count").Remove();
            jObj.Property("custom_status2_count").Remove();
            jObj.Property("custom_status3_count").Remove();
            jObj.Property("custom_status4_count").Remove();
            jObj.Property("custom_status5_count").Remove();
            jObj.Property("custom_status6_count").Remove();
            jObj.Property("custom_status7_count").Remove();
            jObj.Property("created_on").Remove();
            jObj.Property("created_by").Remove();
            return jObj;
        }

        public JArray ParseRuns(JArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                JObject arrayObject = array[i].ToObject<JObject>();
                arrayObject = ParseRun(arrayObject);
            }
            return array;
        }

		public JObject ParseCase(JObject jObj)
		{
			jObj.Property("created_on").Remove();
			jObj.Property("created_by").Remove();
			jObj.Property("estimate").Remove();
			jObj.Property("estimate_forecast").Remove();
			jObj.Property("milestone_id").Remove();
			jObj.Property("priority_id").Remove();
			jObj.Property("template_id").Remove();
			jObj.Property("type_id").Remove();
            jObj.Property("section_id").Remove();
            jObj.Property("suite_id").Remove();
			jObj.Property("updated_by").Remove();
			jObj.Property("updated_on").Remove();
			return jObj;
		}

		public JArray ParseCases(JArray array)
		{
			for (int i = 0; i < array.Count; i++)
			{
				JObject arrayObject = array[i].ToObject<JObject>();
                arrayObject = ParseCase(arrayObject);
			}
			return array;
		}

        public List<Attachment> CreateAttachmentsFromSuites(JArray array)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                attach.Title = jObj.Property("name").Value.ToString();
                attach.TitleLink = jObj.Property("url").Value.ToString();
                attach.Text = "Suite ID: " + jObj.Property("id").Value.ToString() + "\n" + jObj.Property("description").Value.ToString();
                attachments.Add(attach);
            }
            return attachments;
        }

        public List<Attachment> CreateAttachmentsFromSuiteSearch(JArray array, string searchTerm)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                if (jObj.Property("name").Value.ToString().ToLower().Contains(searchTerm.ToLower()))
                {
                    attach.Title = jObj.Property("name").Value.ToString();
                    attach.TitleLink = jObj.Property("url").Value.ToString();
                    attach.Text = "Suite ID: " + jObj.Property("id").Value.ToString() + "\n" + jObj.Property("description").Value.ToString();
                    attachments.Add(attach);
                }
            }
            return attachments;
        }

        public List<Attachment> CreateAttachmentsFromSuiteID(JObject jObj)
        {
            List<Attachment> attachments = new List<Attachment>();
            Attachment attach = new Attachment();
            attach.Title = jObj.Property("name").Value.ToString();
            attach.TitleLink = jObj.Property("url").Value.ToString();
            attach.Text = jObj.Property("description").Value.ToString();
            attachments.Add(attach);
            return attachments;
        }

        public List<Attachment> CreateAttachmentsFromProjects(JArray array)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                attach.Title = jObj.Property("name").Value.ToString();
                attach.TitleLink = jObj.Property("url").Value.ToString();
                attach.Text = "Project ID: " + jObj.Property("id").Value.ToString();
                attachments.Add(attach);
            }
            return attachments;
        }

        public List<Attachment> CreateAttachmentsFromSections(JArray array)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                attach.Title = jObj.Property("name").Value.ToString();
                //attach.TitleLink = jObj.Property("url").Value.ToString();
                attach.Text = "Section ID: " + jObj.Property("id").Value.ToString() + "\n" + jObj.Property("description").Value.ToString();
                attachments.Add(attach);
            }
            return attachments;
        }

		public List<Attachment> CreateAttachmentsFromSectionsNames(JArray array)
		{
			List<Attachment> attachments = new List<Attachment>();
			foreach (JObject jObj in array)
			{
				Attachment attach = new Attachment();
				attach.Text = jObj.Property("name").Value.ToString();
				//attach.TitleLink = jObj.Property("url").Value.ToString();
				//attach.Text = "Section ID = " + jObj.Property("id").Value.ToString() + "\n" + jObj.Property("description").Value.ToString();
				attachments.Add(attach);
			}
			return attachments;
		}

        public List<Attachment> CreateAttachmentsFromPlans(JArray array)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                attach.Title = jObj.Property("name").Value.ToString();
                attach.TitleLink = jObj.Property("url").Value.ToString();

                StringBuilder builder = new StringBuilder();
                builder.Append("Plan ID: ").Append(jObj.Property("id").Value.ToString()).Append("\n");
                builder.Append(jObj.Property("description").Value.ToString()).Append("\n");
                builder.Append("Passed: ").Append(jObj.Property("passed_count").Value.ToString()).Append("\n");
                builder.Append("Failed: ").Append(jObj.Property("failed_count").Value.ToString()).Append("\n");
                builder.Append("Untested: ").Append(jObj.Property("untested_count").Value.ToString());

                attach.Text = builder.ToString();
                attachments.Add(attach);
            }
            return attachments;
        }

        public List<Attachment> CreateAttachmentsFromRuns(JArray array)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                attach.Title = jObj.Property("name").Value.ToString();
                attach.TitleLink = jObj.Property("url").Value.ToString();
                attach.Text = RunText(jObj);

                attachments.Add(attach);
            }
            return attachments;
        }

        public List<Attachment> CreateAttachmentsFromRun(JObject jObj)
        {
            List<Attachment> attachments = new List<Attachment>();
            Attachment attach = new Attachment();
            attach.Title = jObj.Property("name").Value.ToString();
            attach.TitleLink = jObj.Property("url").Value.ToString();
            attach.Text = RunText(jObj);

            attachments.Add(attach);
            return attachments;
        }

        public List<Attachment> CreateAttachmentsFromCloseRun(JObject jObj, JArray jArr)
		{
			List<Attachment> attachments = new List<Attachment>();
			Attachment attach = new Attachment();

            List<string> sectionNames = new List<string>();
            foreach (JObject obj in jArr)
            {
                sectionNames.Add(obj.Property("name").Value.ToString());
            }

            StringBuilder builder = new StringBuilder();
            foreach (string sectionName in sectionNames)
            {
                builder.Append(sectionName).Append("\n");
            }

            Attachment sectionAttachment = new Attachment();

            sectionAttachment.Title = "Sections covered in this run:";
            sectionAttachment.Text = builder.ToString();

			attach.Title = jObj.Property("name").Value.ToString();
			attach.TitleLink = jObj.Property("url").Value.ToString();
			attach.Text = RunText(jObj);

			attachments.Add(attach);
            attachments.Add(sectionAttachment);
			return attachments;
		}

        public List<Attachment> CreateAttachmentsFromRunSearch(JArray array, string searchTerm)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                if (jObj.Property("name").Value.ToString().ToLower().Contains(searchTerm.ToLower()))
                {
                    attach.Title = jObj.Property("name").Value.ToString();
                    attach.TitleLink = jObj.Property("url").Value.ToString();
                    attach.Text = RunText(jObj);

                    attachments.Add(attach);
                }
            }
            return attachments;
        }

        public List<Attachment> CreateAttachmentsFromRunToday(JArray array, string searchTerm)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (JObject jObj in array)
            {
                Attachment attach = new Attachment();
                if (!string.IsNullOrEmpty(jObj.Property("completed_on").Value.ToString()))
                {
                    if (jObj.Property("name").Value.ToString().ToLower().Contains(searchTerm.ToLower()))
                    {
                        DateTimeOffset completedOn = DateTimeOffset.FromUnixTimeSeconds(jObj.Property("completed_on").Value.ToObject<long>());
                        DateTime completedOnDateTime = completedOn.DateTime;
                        if (completedOnDateTime >= DateTime.Today)
                        {
                            attach.Title = jObj.Property("name").Value.ToString();
                            attach.TitleLink = jObj.Property("url").Value.ToString();
                            attach.Text = RunText(jObj);

                            attachments.Add(attach);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            return attachments;
        }

		public List<Attachment> CreateAttachmentsFromCases(JArray array)
		{
			List<Attachment> attachments = new List<Attachment>();
			foreach (JObject jObj in array)
			{
				Attachment attach = new Attachment();
				attach.Title = jObj.Property("title").Value.ToString();

                attach.Text = "References: " + jObj.Property("refs").Value.ToString();
				attachments.Add(attach);
			}
			return attachments;
		}

        public string RunText(JObject jObj)
        {
			StringBuilder builder = new StringBuilder();

			if (!string.IsNullOrEmpty(jObj.Property("plan_id").Value.ToString()))
			{
				builder.Append("Plan ID: ").Append(jObj.Property("plan_id").Value.ToString()).Append("\n");
			}
			else
			{
				builder.Append("Plan ID: N/A").Append("\n");
			}

			builder.Append("Run ID: ").Append(jObj.Property("id").Value.ToString()).Append("\n");
			builder.Append("Suite ID: ").Append(jObj.Property("suite_id").Value.ToString()).Append("\n");
			builder.Append(jObj.Property("description").Value.ToString()).Append("\n");
			builder.Append("Is Completed: ").Append(jObj.Property("is_completed").Value.ToString()).Append("\n");
			builder.Append("Passed: ").Append(jObj.Property("passed_count").Value.ToString()).Append("\n");
			builder.Append("Failed: ").Append(jObj.Property("failed_count").Value.ToString()).Append("\n");
			builder.Append("Untested: ").Append(jObj.Property("untested_count").Value.ToString());

            return builder.ToString();
        }

        public List<List<Attachment>> SplitList(List<Attachment> listOfAttachments, int nSize)
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