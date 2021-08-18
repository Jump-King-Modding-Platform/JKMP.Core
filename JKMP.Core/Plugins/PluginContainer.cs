namespace JKMP.Core.Plugins
{
    public class PluginContainer
    {
        public Plugin Plugin { get; }
        public PluginInfo Info { get; }
        public IPluginLoader Loader { get; }

        public PluginContainer(Plugin plugin, PluginInfo info, IPluginLoader loader)
        {
            Plugin = plugin;
            Info = info;
            Loader = loader;
        }
    }
}