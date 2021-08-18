using System.Collections.Generic;

namespace JKMP.Core.Plugins
{
    public interface IPluginLoader
    {
        public ICollection<string> SupportedExtensions { get; }

        PluginContainer LoadPlugin(string filePath, PluginInfo pluginInfo);
    }
}