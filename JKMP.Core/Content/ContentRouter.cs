using System.IO;
using System.Linq;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using Serilog;

namespace JKMP.Core.Content
{
    internal static class ContentRouter
    {
        private static ILogger Logger = LogManager.CreateLogger(typeof(ContentRouter));

        public static string GetContentPath(string assetName)
        {
            foreach (PluginContainer container in JKCore.Instance.Plugins)
            {
                string contentRoot = container.ContentRoot;

                string pluginAssetPath = Path.Combine(contentRoot, assetName);

                void LogRedirect()
                {
                    Logger.Verbose("Redirecting asset (\"{originalAssetName}\" -> \"{newAssetName}\")", assetName, pluginAssetPath);
                }

                if (File.Exists(pluginAssetPath))
                {
                    LogRedirect();
                    return pluginAssetPath;
                }

                if (File.Exists(Path.ChangeExtension(pluginAssetPath, ".xnb")))
                {
                    LogRedirect();
                    return pluginAssetPath;
                }
            }
            
            return assetName;
        }
    }
}