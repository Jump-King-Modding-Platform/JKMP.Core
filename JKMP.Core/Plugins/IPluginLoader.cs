using System.Collections.Generic;

namespace JKMP.Core.Plugins
{
    /// <summary>
    /// This class is used to find and load plugins at application startup.
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