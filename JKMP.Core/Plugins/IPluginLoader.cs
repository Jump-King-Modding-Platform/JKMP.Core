using System.Collections.Generic;

namespace JKMP.Core.Plugins
{
    /// <summary>
    /// This class is used to find and load plugins at application startup.
    /// The name of the class must be suffixed by "PluginLoader", and start with the name of the loader (e.g. "MyLoader").
    /// Note that the assembly name must match JKMP.*Loader.MyLoader and end with the name of the plugin (e.g. "JKMP.Loader.MyLoader.dll").
    /// </summary>
    public interface IPluginLoader
    {
        /// <summary>
        /// Gets a collection of all supported file extensions that this loader supports.
        /// </summary>
        public ICollection<string> SupportedExtensions { get; }

        /// <summary>
        /// Called by the plugin manager for each plugin that is found that has a supported file extension.
        /// When called by the <see cref="PluginManager"/> the file at the file path is guaranteed to exist.
        /// </summary>
        /// <param name="filePath">The path to the plugin's entry point. When called by the <see cref="PluginManager"/> the file is guaranteed to exist and have one of the supported extensions.</param>
        /// <param name="pluginInfo">The meta data contained within plugin.json</param>
        /// <returns></returns>
        PluginContainer LoadPlugin(string filePath, PluginInfo pluginInfo);
    }
}