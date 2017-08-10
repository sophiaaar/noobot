﻿using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Noobot.Core.Configuration
{
    public class ConfigReader : IConfigReader
    {
        private JObject _currentJObject;
        private readonly string _configLocation;
        private readonly object _lock = new object();
        private const string DEFAULT_LOCATION = @"configuration/config.json"; //TODO - figure out how to make this work for mac and pc!
        private const string SLACKAPI_CONFIGVALUE = "slack:apiToken";

        public ConfigReader() : this(DEFAULT_LOCATION) { }
        public ConfigReader(string configurationFile)
        {
            _configLocation = configurationFile;
        }

        public bool HelpEnabled { get; set; } = true;
        public bool StatsEnabled { get; set; } = true;
        public bool AboutEnabled { get; set; } = true;
        public bool TestrailEnabled { get; set; } = true;
        public bool HelloEnabled { get; set; } = true;

        public string SlackApiKey => GetConfigEntry<string>(SLACKAPI_CONFIGVALUE);

        public T GetConfigEntry<T>(string entryName)
        {
            return GetJObject().Value<T>(entryName);
        }

        private JObject GetJObject()
        {
            lock (_lock)
            {
                if (_currentJObject == null)
                {
                    string assemblyLocation = AssemblyLocation();
                    string fileName = Path.Combine(assemblyLocation, _configLocation);
                    string json = File.ReadAllText(fileName);
                    _currentJObject = JObject.Parse(json);
                }
            }

            return _currentJObject;
        }

        private string AssemblyLocation()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var codebase = new Uri(assembly.CodeBase);
            var path = Path.GetDirectoryName(codebase.LocalPath);
            return path;
        }
    }
}