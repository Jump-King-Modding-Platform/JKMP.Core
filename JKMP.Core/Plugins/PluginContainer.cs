namespace JKMP.Core.Plugins
{
    public class PluginContainer
    {
        public Plugin Plugin { get; }
        public PluginInfo Info { get; }
        public IPluginLoader? Loader { get; }
        public string RootDirectory { get; internal set; } = null!;
        public string ContentRoot { get; internal set; } = null!;

        public PluginContainer(Plugin plugin, PluginInfo info, IPluginLoader? loader)
        {
            Plugin = plugin;
            Info = info;
            Loader = loader;
        }
    }
}