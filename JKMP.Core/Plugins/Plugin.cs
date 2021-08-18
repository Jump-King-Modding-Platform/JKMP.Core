namespace JKMP.Core.Plugins
{
    public abstract class Plugin
    {
        /// <summary>
        /// Called after all plugins have been loaded.
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// Called right after the plugin was loaded.
        /// </summary>
        public virtual void OnLoaded() { }
    }
}