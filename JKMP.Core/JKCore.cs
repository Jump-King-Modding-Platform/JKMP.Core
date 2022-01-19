using System;
using HarmonyLib;
using JKMP.Core.Content;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using Serilog;

namespace JKMP.Core
{
    /// <summary>
    /// The core of JKMP. This class is mainly responsible for loading all plugins.
    /// </summary>
    public sealed class JKCore
    {
        /// <summary>
        /// Gets the plugin manager that handles all plugins.
        /// </summary>
        public PluginManager Plugins { get; }
        
        internal static JKCore Instance { get; private set; } = null!;
        
        private readonly Harmony harmony;

        private static readonly ILogger Logger = LogManager.CreateLogger<JKCore>();

        internal JKCore()
        {
            if (Instance != null)
                throw new InvalidOperationException("There can only be one JKCore instance");
            
            Instance = this;
            
            Logger.Information("Initializing JKMP!");
            
            harmony = new Harmony("com.jkmp.core");
            harmony.PatchAll(typeof(JKCore).Assembly);
            
            Plugins = new();
            Plugins.LoadPlugins();
        }
    }
}