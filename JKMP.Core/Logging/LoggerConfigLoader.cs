using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace JKMP.Core.Logging
{
    internal class LoggerConfigLoader : ILoggerSettings
    {
        internal class Config
        {
            public LogEventLevel MinimumLogLevel { get; set; } = LogEventLevel.Information;

            public string OutputTemplate { get; set; } =
                "[{@t:HH:mm:ss} {@l:u3} {#if SourceContext is not null}{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}{#end}] {@m}\n{#if @x is not null}{@x}\n{#end}";
        }
        
        public Config LogConfig { get; }

        public LoggerConfigLoader()
        {
            string logConfigFileName = Path.Combine("JKMP", "LogConfig.json");

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
                    LogConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(logConfigFileName), serializerSettings)!;
                }
                catch (Exception ex)
                {
                    // ignore
                }
            }

            LogConfig ??= new();
            
            // Write config to disk in case it's new or there's new properties since last saved version
            string json = JsonConvert.SerializeObject(LogConfig, Formatting.Indented, serializerSettings);
            File.WriteAllText(logConfigFileName, json);
        }

        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            loggerConfiguration
                .MinimumLevel.Is(LogConfig.MinimumLogLevel);
        }
    }
}