using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using NewRelic.Platform.Sdk.Extensions;
using NewRelic.Platform.Sdk.Utils;

namespace NewRelic.Platform.Sdk
{
    /// <summary>
    /// An abstract utility class provided to easily create Agents from a configuration file
    /// </summary>
    public abstract class AgentFactory
    {
        private static readonly string ConfigurationFilePath = Path.Combine("config", "plugin.json");

        private static Logger s_log = Logger.GetLogger("AgentFactory");

        internal List<Agent> CreateAgents()
        {
            List<object> agentConfigurations = ReadJsonFile();
            List<Agent> agents = new List<Agent>();

            foreach (object properties in agentConfigurations)
            {
                agents.Add(CreateAgentWithConfiguration((IDictionary<string, object>)properties));
            }

            return agents;
        }

        internal List<object> ReadJsonFile()
        {
            if (!File.Exists(ConfigurationFilePath))
            {
                throw new FileNotFoundException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Unable to locate plugin configuration file at {0}",
                    Path.GetFullPath(ConfigurationFilePath)));
            }

            string localPath = Path.Combine(Assembly.GetExecutingAssembly().GetLocalPath(), ConfigurationFilePath);
            IDictionary<string, object> configContents = JsonHelper.Deserialize(File.ReadAllText(ConfigurationFilePath)) as IDictionary<string, object>;
            List<object> agentProperties;

            if (configContents != null)
            {
                agentProperties = configContents["agents"] as List<object>;
            }
            else
            {
                agentProperties = null;
            }

            if (agentProperties == null)
            {
                throw new ConfigurationErrorsException("The contents of 'plugin.json' are invalid. Please ensure you have a root 'agents' property that contains an array of JSON objects.");
            }

            return agentProperties;
        }

        /// <summary>
        /// The AgentFactory will read configuration data from specified JSON file and invoke this method for each
        /// object configuration containing an IDictionary of the deserialized properties
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public abstract Agent CreateAgentWithConfiguration(IDictionary<string, object> properties);
    }
}
