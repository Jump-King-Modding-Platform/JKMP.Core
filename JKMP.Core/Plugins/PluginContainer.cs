using System.IO;

namespace JKMP.Core.Plugins
{
    public class PluginContainer
    {
        public Plugin Plugin { get; }
        public PluginInfo Info { get; }
        public IPluginLoader? Loader { get; }
        public string RootDirectory { get; }
        public string ContentRoot { get; }

        public PluginContainer(Plugin plugin, PluginInfo info, IPluginLoader? loader, string rootDirectory)
        {
            Plugin = plugin;
            Info = info;
            Loader = loader;
            RootDirectory = rootDirectory;
            ContentRoot = Path.Combine(RootDirectory, "Content");
        }
    }
}