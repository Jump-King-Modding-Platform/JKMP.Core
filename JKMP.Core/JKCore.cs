using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JKMP.Core.Configuration;
using JKMP.Core.Content;
using JKMP.Core.Input;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Semver;
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
        
        /// <summary>
        /// Gets the version of Core.
        /// </summary>
        public SemVersion Version { get; }
        
        internal JkmpConfig Config { get; }

        internal PluginConfigs InternalConfigs { get; }

        /// <summary>
        /// Gets the JKCore singleton. It is the base of the Core framework.
        /// </summary>
        public static JKCore Instance { get; private set; } = null!;
        
        private readonly Harmony harmony;
        private StartupInformation? startupInformation;

        private static readonly ILogger Logger = LogManager.CreateLogger<JKCore>();

        internal JKCore()
        {
            if (Instance != null)
                throw new InvalidOperationException("There can only be one JKCore instance");
            
            Instance = this;

            Version = SemVersion.FromVersion(Assembly.GetExecutingAssembly().GetName().Version);

            Logger.Information("Initializing JKMP v{version}!", Version);
            
            harmony = new Harmony("com.jkmp.core");
            harmony.PatchAll(typeof(JKCore).Assembly);
            
            InternalConfigs = new PluginConfigs(Plugin.InternalPlugin)
            {
                JsonSerializerSettings = PluginManager.CreateDefaultJsonSerializerSettings()
            };
            Config = InternalConfigs.LoadConfig<JkmpConfig>("Config");

            InputManager.CreateVanillaKeyBinds();
            
            Plugins = new();
            Plugins.LoadPlugins();

            InputManager.Initialize();
            
            Events.PostGameInitialized += OnPostGameInitialized;
            Events.PreGameUpdate += OnPreGameUpdate;
            Events.PostGameUpdate += OnPostGameUpdate;
            Events.GameTitleScreenLoaded += OnGameTitleScreenLoaded;
        }

        internal void SaveConfig()
        {
            InternalConfigs.SaveConfig(Config, "Config");
        }

        private void OnPostGameInitialized(object sender, EventArgs e)
        {
            
        }

        private void OnGameTitleScreenLoaded(object sender, EventArgs e)
        {
            startupInformation = new();
        }

        private void OnPreGameUpdate(object sender, float delta)
        {
            InputManager.Update();
        }

        private void OnPostGameUpdate(object sender, float delta)
        {
            if (startupInformation?.Update(delta) == true)
            {
                startupInformation = null;
            }
        }
    }
}