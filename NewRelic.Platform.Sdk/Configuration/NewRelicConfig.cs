using System.Globalization;
using System.IO;
using System.Reflection;
using NewRelic.Platform.Sdk.Extensions;
using Newtonsoft.Json;

namespace NewRelic.Platform.Sdk.Configuration
{
    public class NewRelicConfig : INewRelicConfig
    {
        private static NewRelicConfig ConfigInstance;
        private static readonly string ConfigPath = Path.Combine("config", "newrelic.json");
        private const string DefaultEndpoint = "https://platform-api.newrelic.com/platform/v1/metrics";
        private const string DefaultLogFileName = "newrelic_plugin.log";
        private const string DefaultLogFilePath = @"logs";
        private const int DefaultLogLimitInKiloBytes = 25600;

        private NewRelicConfig()
        {
            // set default values
            this.Endpoint = DefaultEndpoint;
            this.LogLevel = LogLevel.Info;
            this.LogFileName = DefaultLogFileName;
            this.LogFilePath = Path.Combine(Assembly.GetExecutingAssembly().GetLocalPath(), DefaultLogFilePath);
            this.LogLimitInKiloBytes = DefaultLogLimitInKiloBytes;
        }

        [JsonProperty(PropertyName = "license_key")]
        public string LicenseKey { get; set; }

        [JsonProperty(PropertyName = "endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty(PropertyName = "log_level")]
        public LogLevel LogLevel { get; set; }

        [JsonProperty(PropertyName = "log_filename")]
        public string LogFileName { get; set; }

        [JsonProperty(PropertyName = "log_file_path")]
        public string LogFilePath { get; set; }

        [JsonProperty(PropertyName = "log_limit_in_kbytes")]
        public long LogLimitInKiloBytes { get; set; }

        [JsonProperty(PropertyName = "proxy_host")]
        public string ProxyHost { get; set; }

        [JsonProperty(PropertyName = "proxy_port")]
        public int? ProxyPort { get; set; }

        [JsonProperty(PropertyName = "proxy_username")]
        public string ProxyUserName { get; set; }

        [JsonProperty(PropertyName = "proxy_password")]
        public string ProxyPassword { get; set; }

        [JsonProperty(PropertyName = "poll_interval")]
        public int? PollInterval { get; set; }

        // Exposed for testing
        public int? NewRelicMaxIterations { get; set; }

        public static NewRelicConfig Instance
        {
            get
            {
                if (ConfigInstance == null)
                {
                    string assemblyPath = Assembly.GetExecutingAssembly().GetLocalPath();
                    string configPath = Path.Combine(assemblyPath, ConfigPath);

                    if (!File.Exists(configPath))
                    {
                        throw new FileNotFoundException(string.Format(
                            CultureInfo.InvariantCulture,
                            "New relic configuration needs to be located at {0}",
                            configPath));
                    }

                    ConfigInstance = JsonConvert.DeserializeObject<NewRelicConfig>(File.ReadAllText(configPath));
                }

                return ConfigInstance;
            }
        }
    }
}
