using System;
using System.IO;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace JKMP.Core.Logging
{
    /// <summary>
    /// Handles application logging, you should use this when you want to log something instead of using Console.WriteLine for example.
    /// </summary>
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

        /// <summary>
        /// Gets a logger that is used for temporary logging. Useful for debugging.
        /// </summary>
        public static ILogger TempLogger { get; private set; } = null!;
        
        internal static void InitializeLogging()
        {
            Directory.CreateDirectory(Path.Combine("JKMP", "Logs"));

            LoggerConfigLoader loggerSettings = new();
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Settings(loggerSettings)
                .WriteTo.Console(new ExpressionTemplate(loggerSettings.LogConfig.OutputTemplate, theme: TemplateTheme.Code, applyThemeWhenOutputIsRedirected: true))
                .WriteTo.File(new ExpressionTemplate(loggerSettings.LogConfig.OutputTemplate), Path.Combine("JKMP", "Logs", "jkmp_.log"), rollingInterval: RollingInterval.Day)
                .Enrich.WithDemystifiedStackTraces()
                .CreateLogger();

            TempLogger = Log.Logger.ForContext<TempLog>();
        }
    }

    /// <summary>
    /// Used for identifying temporary logging.
    /// </summary>
    internal class TempLog
    {
    }
}