using System;
using System.IO;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace JKMP.Core.Logging
{
    public static class LogManager
    {
        /// <summary>
        /// Creates a logger for the specified type.
        /// </summary>
        /// <typeparam name="T">The type that will use this logger. Used for knowing where a log message came from.</typeparam>
        /// <returns></returns>
        public static ILogger CreateLogger<T>()
        {
            return Log.Logger.ForContext<T>();
        }

        /// <summary>
        /// Creates a logger for the specified type. This overload is normally used for static classes where it can't be used as a generic type.
        /// </summary>
        /// <param name="type">The type that will use this logger. Used for knowing where a log message came from.</param>
        /// <returns></returns>
        public static ILogger CreateLogger(Type type)
        {
            return Log.Logger.ForContext(type);
        }
        
        internal static void InitializeLogging()
        {
            Directory.CreateDirectory(Path.Combine("JKMP", "Logs"));

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Settings(new LoggerConfigLoader())
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true)
                .WriteTo.File(Path.Combine("JKMP", "Logs", "jkmp.log"), restrictedToMinimumLevel: LogEventLevel.Information, rollingInterval: RollingInterval.Day)
                .Enrich.WithDemystifiedStackTraces()
                .CreateLogger();
        }
    }
}