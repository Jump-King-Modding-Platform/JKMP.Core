using System;
using HarmonyLib;
using JKMP.Core.Content;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using Serilog;

namespace JKMP.Core
{
    public sealed class JKCore
    {
        public PluginManager Plugins { get; }
        
        internal static JKCore Instance { get; private set; }
        
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