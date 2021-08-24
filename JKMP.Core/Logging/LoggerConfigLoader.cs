using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace JKMP.Core.Logging
{
    public class LoggerConfigLoader : ILoggerSettings
    {
        private class Config
        {
            [JsonProperty()]
            public LogEventLevel MinimumLogLevel { get; set; } = LogEventLevel.Information;
        }
        
        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            string logConfigFileName = Path.Combine("JKMP", "LogConfig.json");

            Config? config = null;
            JsonSerializerSettings serializerSettings = new()
            {
                Converters =
                {
                    new StringEnumConverter()
                }
            };
            
            if (File.Exists(logConfigFileName))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(logConfigFileName), serializerSettings)!;
                }
                catch (Exception ex)
                {
                    // ignore
                }
            }

            if (config == null)
            {
                // Write new config to disk
                config = new();
                
                string json = JsonConvert.SerializeObject(config, Formatting.Indented, serializerSettings);
                File.WriteAllText(logConfigFileName, json);
            }

            loggerConfiguration
                .MinimumLevel.Is(config.MinimumLogLevel);
        }
    }
}