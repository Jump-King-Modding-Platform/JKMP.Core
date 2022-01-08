using JKMP.Core.Configuration;
using Newtonsoft.Json;

namespace JKMP.Core.Plugins
{
    public abstract class Plugin
    {
        public PluginContainer Container { get; internal set; } = null!;
        public PluginInfo Info => Container.Info;
        
        /// <summary>
        /// Gets the configuration manager for this plugin.
        /// </summary>
        public PluginConfigs Configs { get; internal set; } = null!;
        
        /// <summary>
        /// Called after all plugins have been loaded.
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// Called right after the plugin was loaded.
        /// </summary>
        public virtual void OnLoaded() { }

        /// <summary>
        /// Sets the json serialization settings that is used when serializing and deserializing json files.
        /// </summary>
        /// <param name="settings">The new settings to use. Can be null to reset to default settings.</param>
        protected void SetJsonSerializationSettings(JsonSerializerSettings? settings)
        {
            Configs.JsonSerializerSettings = settings ?? PluginManager.CreateDefaultJsonSerializerSettings();
        }
    }
}