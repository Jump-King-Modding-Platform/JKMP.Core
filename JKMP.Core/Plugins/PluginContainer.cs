namespace JKMP.Core.Plugins
{
    /// <summary>
    /// Holds information about a plugin.
    /// </summary>
    public class PluginContainer
    {
        /// <summary>
        /// Gets the contained plugin.
        /// </summary>
        public Plugin Plugin { get; }
        
        /// <summary>
        /// Gets the metadata that is contained within plugin.json.
        /// </summary>
        public PluginInfo Info { get; }
        
        /// <summary>
        /// Gets the plugin loader that was used to load this plugin.
        /// </summary>
        public IPluginLoader? Loader { get; }
        
        /// <summary>
        /// Gets the path to the plugin relative to the game's root directory.
        /// </summary>
        public string RootDirectory { get; internal set; } = null!;
        
        /// <summary>
        /// Gets the path to the plugin's content directory relative to the game's root directory.
        /// </summary>
        public string ContentRoot { get; internal set; } = null!;
        
        /// <summary>
        ///  Gets the path to the plugin's config directory relative to the game's root directory.
        /// </summary>
        public string ConfigRoot { get; internal set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginContainer"/> class. Usually only used by plugin loaders.
        /// </summary>
        /// <param name="plugin">The loaded plugin.</param>
        /// <param name="info">The metadata of the plugin, usually loaded from a plugin's manifest file 'plugin.json'.</param>
        /// <param name="loader">The loaded that loaded the plugin.</param>
        public PluginContainer(Plugin plugin, PluginInfo info, IPluginLoader? loader)
        {
            Plugin = plugin;
            Info = info;
            Loader = loader;
        }
    }
}