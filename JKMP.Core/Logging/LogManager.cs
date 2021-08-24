using System.IO;
using Serilog;
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
        
        internal static void InitializeLogging()
        {
            Directory.CreateDirectory(Path.Combine("JKMP", "Logs"));

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true)
                .WriteTo.File(Path.Combine("JKMP", "Logs", "jkmp.log"), restrictedToMinimumLevel: LogEventLevel.Information, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}