﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewRelic.Platform.Sdk.Binding
{
    public interface IContext
    {
        string Version { get; set; }

        /// <summary>
        /// Prepare a New Relic metric to be delivered to the service at the end of this poll cycle.  
        /// </summary>
        /// <param name="guid">A string guid identifying the plugin this is associated with (e.g. 'com.yourcompany.pluginname')</param>
        /// <param name="componentName">A string name identifying the plugin component that will appear in the service UI (e.g. 'MyPlugin')</param>
        /// <param name="metricName">A string name representing the name of the metric (e.g. 'Category/Name')</param>
        /// <param name="units">The units of the metric you are sending to the service (e.g. 'byte/second')</param>
        /// <param name="value">The non-negative float value representing this value</param>
        void ReportMetric(string guid, string componentName, string metricName, string units, float? value);

        /// <summary>
        /// Will send all metrics that were reported to the Context since its last successful delivery to the New Relic service.
        /// The context will aggregate values for calls that fail due to transient issues.
        /// </summary>
        void SendMetricsToService();
    }
}
