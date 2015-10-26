using System;
using NewRelic.Platform.Sdk.Binding;

namespace NewRelic.Platform.Sdk
{
    /// <summary>
    /// An abstract utility class to programmatically create Agents.  
    /// Each agent will have its PollCycle method invoked once per poll interval.
    /// </summary>
    public abstract class Agent
    {
        public abstract string Guid { get; }
        public abstract string Version { get; }

        private IContext _context;

        public Agent()
        {
        }

        /// <summary>
        /// Each Agent that shares a Context reference will be sent in a single request.
        /// </summary>
        /// <param name="context"></param>
        public void PrepareToRun(IContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context", "You must pass an initialized context when calling PrepareToRun()");
            }

            _context = context;

            if(string.IsNullOrEmpty(_context.Version))
            {
                _context.Version = Version;
            }
        }

        /// <summary>
        /// Registers a metric for this poll cycle that will be sent when all Agents complete their PollCycle.
        /// </summary>
        /// <param name="metricName"></param>
        /// <param name="units"></param>
        /// <param name="value"></param>
        public void ReportMetric(string metricName, string units, float? value)
        {
            _context.ReportMetric(this.Guid, this.GetAgentName(), metricName, units, value);
        }

        /// <summary>
        /// Registers a metric for this poll cycle that will be sent when all Agents complete their PollCycle.
        /// </summary>
        /// <param name="metricName">A string name representing the name of the metric (e.g. 'Category/Name')</param>
        /// <param name="units">The units of the metric you are sending to the service (e.g. 'byte/second')</param>
        /// <param name="value">The non-negative float value representing this value</param>
        /// <param name="count">The int value representing how many poll cycle this metric has been aggregated for</param>
        /// <param name="min">The non-negative float value representing this min value for this poll cycle</param>
        /// <param name="max">The non-negative float value representing this max value for this poll cycle</param>
        /// <param name="sumOfSquares">The non-negative float value representing the sum of square values for this poll cycle</param>
        public void ReportMetric(string metricName, string units, float value, int count, float min, float max, float sumOfSquares)
        {
            _context.ReportMetric(Guid, GetAgentName(), metricName, units, value, count, min, max, sumOfSquares);
        }

        /// <summary>
        /// Each descended class has this method invoked once per poll interval.  Consumers should invoke ReportMetric() 
        /// within this method in order to have metrics sent to the service.
        /// </summary>
        public abstract void PollCycle();

        /// <summary>
        /// A human readable string denotes the name of this Agent in the New Relic service.
        /// </summary>
        /// <returns></returns>
        public abstract string GetAgentName();
    }
}
