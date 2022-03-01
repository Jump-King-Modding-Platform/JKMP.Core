using JKMP.Core.Configuration;
using JKMP.Core.Input;
using Newtonsoft.Json;

namespace JKMP.Core.Plugins
{
    /// <summary>
    /// The main plugin class. It contains the main entry point for all plugins.
    /// The name of the class must be suffixed by "Plugin", and start with the name of the plugin (e.g. "MyPlugin").
    /// Note that the assembly name must start with JKMP.Plugin and end with the name of the plugin (e.g. "JKMP.Plugin.MyPlugin").
    /// It is not recommended to suffix the plugin name with "Plugin" since it is self explanatory and would force the plugin class to be named MyPluginPlugin.
    /// </summary>
    public abstract class Plugin
    {
        /// <summary>
        /// Gets the container that holds this plugin.
        /// Contains various information about this plugin, such as the relative path (from game root) to the content/configuration/root directory.
        /// </summary>
        public PluginContainer Container { get; internal set; } = null!;
        
        /// <summary>
        /// Gets the metadata of this plugin contained within plugin.json.
        /// </summary>
        public PluginInfo Info => Container.Info;
        
        /// <summary>
        /// Gets the configuration manager for this plugin.
        /// </summary>
        public PluginConfigs Configs { get; internal set; } = null!;

        /// <summary>
        /// Gets the input manager for this plugin. It can be used to register input bindings.
        /// Registered input bindings will be configurable in-game.
        /// Note that registering input bindings must be done during initialization, preferably from overriding the <see cref="CreateInputActions"/> method.
        /// An exception will be thrown if an attempt to register input bindings is made after initialization.
        /// </summary>
        public PluginInput Input { get; internal set; } = null!;
        
        /// <summary>
        /// Called after all plugins have been loaded.
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// Called right after the plugin was loaded.
        /// </summary>
        public virtual void OnLoaded() { }
        
        /// <summary>
        /// <para>
        /// Called when the plugin should create its input actions.
        /// </para>
        /// Example usage:
        /// <code>
        /// Input.RegisterAction("Jump", "space");
        /// Input.RegisterAction("WalkLeft", "Walk left", "a");
        ///
        /// // Later on in the game...
        /// Input.BindAction("Jump", pressed => { /* do something */ });
        /// </code>
        /// </summary>
        public virtual void CreateInputActions() { }

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