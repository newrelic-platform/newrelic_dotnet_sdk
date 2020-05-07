using System;
using System.IO;
using System.Net;
using NewRelic.Platform.Sdk.Configuration;
using NewRelic.Platform.Sdk.Utils;

namespace NewRelic.Platform.Sdk.Binding
{
    public class Context : IContext
    {
        private RequestData _requestData;
        private string _licenseKey;
        private readonly INewRelicConfig _newRelicConfig;

        private static Logger s_log = Logger.GetLogger("Context");

        internal string ServiceUri
        {
            get
            {
                return this._newRelicConfig.Endpoint;
            }
        }

        internal string LicenseKey
        {
            get
            {
                return _licenseKey ?? this._newRelicConfig.LicenseKey;
            }
        }

        public string Version
        {
            get
            {
                return _requestData.Version;
            }
            set
            {
                _requestData.Version = value;
            }
        }

        // Exposed to enable functional testing for plugin developers
        public RequestData RequestData
        {
            get
            {
                return _requestData;
            }
        }

        /// <summary>
        /// This class is responsible for maintaining metrics that have been reported as well as sending them to the New Relic 
        /// service. Any Components that share a Request reference will have their data sent to the service in one request.
        /// </summary>
        /// <param name="config">The configuration that should be used.</param>
        public Context(INewRelicConfig config = null)
            : this(null, config)
        {
        }

        /// <summary>
        /// This class is responsible for maintaining metrics that have been reported as well as sending them to the New Relic 
        /// service. Any Components that share a Request reference will have their data sent to the service in one request.
        /// </summary>
        /// <param name="licenseKey">The New Relic license key.</param>
        /// <param name="config">The configuration that should be used.</param>
        public Context(string licenseKey, INewRelicConfig config = null)
        {
            this._newRelicConfig = config ?? NewRelicConfig.Instance;

            this._licenseKey = licenseKey;
            this._requestData = new RequestData();

            s_log.Debug("Using service URL: {0}", this.ServiceUri);
            s_log.Debug("Using license key: {0}", this.LicenseKey);
        }

        /// <summary>
        /// Prepare a New Relic metric to be delivered to the service at the end of this poll cycle.  
        /// </summary>
        /// <param name="guid">A string guid identifying the plugin this is associated with (e.g. 'com.yourcompany.pluginname')</param>
        /// <param name="componentName">A string name identifying the plugin agent that will appear in the service UI (e.g. 'MyPlugin')</param>
        /// <param name="metricName">A string name representing the name of the metric (e.g. 'Category/Name')</param>
        /// <param name="units">The units of the metric you are sending to the service (e.g. 'byte/second')</param>
        /// <param name="value">The non-negative float value representing this value</param>
        public void ReportMetric(string guid, string componentName, string metricName, string units, float? value)
        {
            s_log.Debug("Reporting metric: {0} -> {1}[{2}]={3}", componentName, metricName, units, value);
            if (string.IsNullOrEmpty(guid))
            {
                throw new ArgumentNullException("guid", "Null parameter was passed to ReportMetric()");
            }

            if (string.IsNullOrEmpty(componentName))
            {
                throw new ArgumentNullException("componentName", "Null parameter was passed to ReportMetric()");
            }

            if (string.IsNullOrEmpty(metricName))
            {
                throw new ArgumentNullException("metricName", "Null parameter was passed to ReportMetric()");
            }

            if (string.IsNullOrEmpty(units))
            {
                throw new ArgumentNullException("units", "Null parameter was passed to ReportMetric()");
            }

            // Ensure the value is not null since EpochProcessors will return 0 on initial processing
            if (value.HasValue)
            {
                if (value.Value < 0)
                {
                    throw new ArgumentException("New Relic Platform does not currently support negative values", "value");
                }

                _requestData.AddMetric(guid, componentName, metricName, units, value.Value);
            }
        }

        /// <summary>
        /// Prepare a New Relic metric to be delivered to the service at the end of this poll cycle.
        /// </summary>
        /// <param name="guid">A string guid identifying the plugin this is associated with (e.g. 'com.yourcompany.pluginname')</param>
        /// <param name="componentName">A string name identifying the plugin agent that will appear in the service UI (e.g. 'MyPlugin')</param>
        /// <param name="metricName">A string name representing the name of the metric (e.g. 'Category/Name')</param>
        /// <param name="units">The units of the metric you are sending to the service (e.g. 'byte/second')</param>
        /// <param name="value">The non-negative float value representing this value</param>
        /// <param name="count">The int value representing how many poll cycle this metric has been aggregated for</param>
        /// <param name="min">The non-negative float value representing this min value for this poll cycle</param>
        /// <param name="max">The non-negative float value representing this max value for this poll cycle</param>
        /// <param name="sumOfSquares">The non-negative float value representing the sum of square values for this poll cycle</param>
        public void ReportMetric(string guid, string componentName, string metricName, string units, float value, int count, float min, float max, float sumOfSquares)
        {
            s_log.Debug("Reporting metric: {0} -> {1}[{2}]={3}", componentName, metricName, units, value);
            if (string.IsNullOrEmpty(guid))
            {
                throw new ArgumentNullException("guid", "Null parameter was passed to ReportMetric()");
            }

            if (string.IsNullOrEmpty(componentName))
            {
                throw new ArgumentNullException("componentName", "Null parameter was passed to ReportMetric()");
            }

            if (string.IsNullOrEmpty(metricName))
            {
                throw new ArgumentNullException("metricName", "Null parameter was passed to ReportMetric()");
            }

            if (string.IsNullOrEmpty(units))
            {
                throw new ArgumentNullException("units", "Null parameter was passed to ReportMetric()");
            }

            if (count < 1)
            {
                throw new ArgumentException("New Relic Platform does not support count values less than 1", "count");
            }

            // Ensure the value is not null since EpochProcessors will return 0 on initial processing
            if (value < 0 || min < 0 || max < 0 || sumOfSquares < 0)
            {
                throw new ArgumentException("New Relic Platform does not currently support negative values");
            }

            _requestData.AddMetric(guid, componentName, metricName, units, value, count, min, max, sumOfSquares);
        }

        /// <summary>
        /// Will send all metrics that were reported to the Context since its last successful delivery to the New Relic service.
        /// The context will aggregate values for calls that fail due to transient issues.
        /// </summary>
        public void SendMetricsToService()
        {
            s_log.Info("Preparing to send metrics to service");

            // Check to see if the Agent data is valid before sending data
            if (!ValidateRequestData())
            {
                return;
            }

            var request = (HttpWebRequest)WebRequest.Create(this.ServiceUri);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers["X-License-Key"] = this.LicenseKey;
            try
            {
                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    var str = JsonHelper.Serialize(_requestData.Serialize());
                    s_log.Debug("Sending JSON: {0}", str);
                    writer.Write(str);
                }
                HandleServiceResponse(request);
            }
            catch (WebException we)
            {
                // Log web exception
                s_log.Error("No response from server.");
            }

 
        }

        private bool ValidateRequestData()
        {
            // Do not send any data if no agents reported
            if (!_requestData.HasComponents())
            {
                s_log.Info("No metrics reported for this poll cycle, continuing...");
                _requestData.Reset(); // Reset the aggregation timer
                return false;
            }

            // Reset the agent data if we have been aggregating for too long
            if (_requestData.IsPastAggregationLimit())
            {
                s_log.Warn("The service has not been successfully contacted in several minutes.  Starting a fresh aggregation cycle.");
                _requestData.Reset(); // Reset the aggregation timer
                return false;
            }

            return true;
        }

        private void HandleServiceResponse(HttpWebRequest request)
        {
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            s_log.Info("Metrics successfully sent");
                            // Reset aggregation after successful delivery
                            _requestData.Reset();
                        }
                    }
                }
            }
            catch (WebException we)
            {
                if( we.Response != null )
                {
                    using (var response = (HttpWebResponse)we.Response)
                    {
                        using( var reader = new StreamReader( response.GetResponseStream() ) )
                        {
                            var body = reader.ReadToEnd();

                            if( response.StatusCode == HttpStatusCode.ServiceUnavailable )
                            {
                                // Collector is being updated
                                s_log.Info( "New Relic Service is currently undergoing an upgrade" );
                            }
                            else if( response.StatusCode == HttpStatusCode.Forbidden
                                && string.Equals( Constants.DisableNewRelic, body ) )
                            {
                                // Remotely disabled
                                s_log.Fatal( "Remotely disabled by New Relic service" );
                                throw new NewRelicServiceException( response.StatusCode, "Remotely disabled by New Relic Service", we );
                            }
                            else
                            {
                                // Log unknown exception
                                s_log.Error( "Unexpected response from the New Relic service. StatusCode: {0} ({1}), BodyContents: {2}",
                                    response.StatusCode, response.StatusDescription, body );

                                // Rethrow if this is a client exception, keep trying if it is a server error
                                if( (int)response.StatusCode >= 400 && (int)response.StatusCode < 500 )
                                {
                                    throw new NewRelicServiceException( response.StatusCode, body, we );
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Log unknown exception
                    s_log.Error( "No response from the New Relic service." );
                }
            } // End catch()
        } // End HandleServiceResponse()
    } // End Context
}
