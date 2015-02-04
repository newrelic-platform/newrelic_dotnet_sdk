using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using NewRelic.Platform.Sdk.Binding;
using NewRelic.Platform.Sdk.Configuration;
using NewRelic.Platform.Sdk.Utils;

namespace NewRelic.Platform.Sdk
{
    public class Runner
    {
        private List<AgentFactory> _factories;
        private List<Agent> _agents;
        private readonly INewRelicConfig newRelicConfig;

        private static Logger s_log = Logger.GetLogger("Runner");

        public Runner(INewRelicConfig config = null)
        {
            this.newRelicConfig = config ?? NewRelicConfig.Instance;

            _factories = new List<AgentFactory>();
            _agents = new List<Agent>();

            this.SetupProxy(
                this.newRelicConfig.ProxyHost,
                this.newRelicConfig.ProxyPort,
                this.newRelicConfig.ProxyUserName,
                this.newRelicConfig.ProxyPassword);

            // used for testing purposes
            _limit = this.newRelicConfig.NewRelicMaxIterations.GetValueOrDefault();
            _limitRun = this.newRelicConfig.NewRelicMaxIterations.HasValue;
        }

        /// <summary>
        /// Add an instance of an Agent to the Runner.  Any agents added prior to invoking SetupAndRun() will have their
        /// PollCycle() method invoked each polling interval.
        /// </summary>
        /// <param name="agent"></param>
        public void Add(Agent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException("agent", "You must pass in a non-null agent");
            }

            s_log.Info("Adding new agent: {0}", agent.GetAgentName());
            _agents.Add(agent);
        }

        /// <summary>
        /// Add an instance of a factory to the Runner.  Any factories added prior to invoking SetupAndRun() will have
        /// their CreateAgentWithConfiguration() method invoked which will create a list of Agents initialized through
        /// the factory's configuration file that will be used for polling intervals.
        /// </summary>
        /// <param name="factory"></param>
        public void Add(AgentFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory", "You must pass in a non-null factory");
            }

            s_log.Info("Adding new factory {0}", factory.GetType());
            _factories.Add(factory);
        }

        /// <summary>
        /// This method only returns during a fatal error.  It will initialize agents if necessary, and then begin polling once
        /// per configurable PollInterval invoking registered Agent's PollCycle() methods.  Then sending the data to the New Relic service.
        /// </summary>
        public void SetupAndRun()
        {
            if (_factories.Count == 0 && _agents.Count == 0)
            {
                throw new InvalidOperationException("You must first call 'Add()' at least once with a valid factory or agent");
            }

            // Initialize agents if they added an AgentFactory, otherwise they have explicitly added initialized agents already
            if (_factories.Count > 0)
            {
                InitializeFactoryAgents();
            }

            // Initialize agents with the same Context so they aggregate to a single a request
            var context = new Context();

            foreach (var agent in _agents)
            {
                agent.PrepareToRun(context);
            }

            var pollInterval = GetPollInterval(); // Fetch poll interval here so we can report any issues early

            while (true)
            {
                // Invoke each Agent's PollCycle method, logging any exceptions that occur
                try
                {
                    foreach (var agent in _agents)
                    {
                        agent.PollCycle();
                    }
                }
                catch(Exception e) 
                {
                    s_log.Error("Error error occurred during PollCycle", e);
                }

                try 
                {
                    context.SendMetricsToService();

                    // Enables limited runs for tests that want to invoke the service
                    if (_limitRun && --_limit == 0)
                    {
                        return;
                    }

                    int millisecondsToNextInterval = pollInterval - ( ( DateTime.Now.Second * 1000 ) % pollInterval ) - DateTime.Now.Millisecond;
                    Thread.Sleep(millisecondsToNextInterval);
                }
                catch (Exception e)
                {
                    s_log.Fatal("Fatal error occurred. Shutting down the application", e);
                    throw e;
                }
            }
        }

        protected virtual void SetupProxy(string hostname, int? port, string username, string password)
        {
            if (hostname.IsValidString())
            {
                UriBuilder builder = new UriBuilder(hostname);

                if (port.HasValue)
                {
                    builder.Port = port.Value;
                }
                else
                {
                    throw new InvalidOperationException("When setting up a proxy, port is required.");
                }
                
                ICredentials credentials;
                if (username.IsValidString())
                {
                    credentials = new NetworkCredential(username, password);
                }
                else
                {
                    credentials = null;
                }

                IWebProxy proxy = new WebProxy
                {
                    Address = builder.Uri,
                    Credentials = credentials,
                };

                WebRequest.DefaultWebProxy = proxy;
            }
        }

        private void InitializeFactoryAgents()
        {
            foreach (AgentFactory factory in _factories)
            {
                _agents = _agents.Union(factory.CreateAgents()).ToList();
            }
        }

        private int GetPollInterval()
        {
            int pollInterval = 60;
            return pollInterval *= 1000; // Convert to milliseconds since that's what system calls expect;
        }

        #region Test Helpers

        /// <summary>
        /// DO NOT USE: Exposed for test purposes
        /// </summary>
        private int _limit;
        private bool _limitRun;

        internal List<Agent> Agents { get { return _agents; } }

        internal void SetupAndRunWithLimit(int limit) 
        {
            _limitRun = true;
            _limit = limit;
            SetupAndRun();
        }

        #endregion
    }
}
