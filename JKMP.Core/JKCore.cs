using System;
using HarmonyLib;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using Serilog;

namespace JKMP.Core
{
    public sealed class JKCore
    {
        public PluginManager Plugins { get; }

        private static readonly ILogger Logger = LogManager.CreateLogger<JKCore>();

        internal JKCore()
        {
            Logger.Information("Initializing JKMP!");
            Plugins = new();
            Plugins.LoadPlugins();
        }
    }
}