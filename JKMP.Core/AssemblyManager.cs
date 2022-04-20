using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using JKMP.Core.Utility.IO;
using Serilog;

namespace JKMP.Core
{
    internal static class AssemblyManager
    {
        private static readonly ICollection<string> SearchDirectories = new[]
        {
            Path.Combine("JKMP", "Dependencies"),
            Path.Combine("JKMP", "Loaders")
        };

        private static readonly ICollection<string> IgnoredAssemblies = new[]
        {
            "JumpKing.XmlSerializers",
            "JumpKingPlus.XmlSerializers",
            "LanguageJK.XmlSerializers"
        };

        private static readonly Assembly CoreAssembly = Assembly.GetExecutingAssembly();
        private static readonly ILogger Logger = LogManager.CreateLogger(typeof(AssemblyManager));

        /// <summary>
        /// Matches directory that leads to a plugin's directory. Named capture 'pluginName' is the name of the plugin folder.
        /// </summary>
        private static readonly Regex PluginRootPathRgx = new Regex(@"^JKMP[\/\\]Plugins[\/\\](?<pluginName>[^\/\\\s]+)[\/\\]?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        
        /// <summary>
        /// Matches a directory that points to the root of a plugin's directory. Named capture 'pluginName' is the name of the plugin folder.
        /// </summary>
        private static readonly Regex PluginPathRgx = new Regex($@"{PluginRootPathRgx}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static void SetupAssemblyResolving(AppDomain appDomain)
        {
            appDomain.AssemblyResolve += (sender, args) =>
            {
                Assembly requestingAssembly = args.RequestingAssembly ?? Assembly.GetExecutingAssembly();
                
                var assemblyName = new AssemblyName(args.Name);

                if (IgnoredAssemblies.Contains(assemblyName.Name))
                    return null;
                
                string? requestingAssemblyPath = string.IsNullOrEmpty(requestingAssembly.Location) ? null : Path.GetDirectoryName(requestingAssembly.Location)!;

                if (requestingAssemblyPath != null)
                {
                    requestingAssemblyPath = PathUtility.GetRelativePath(CoreAssembly.Location, requestingAssemblyPath);
                }
                
                string requestingPluginName = requestingAssemblyPath == null ? "" : PluginPathRgx.Match(requestingAssemblyPath ?? "").Groups["pluginName"].Value;
                PluginContainer? requestingPluginContainer = null;

                if (!string.IsNullOrEmpty(requestingPluginName))
                {
                    JKCore.Instance.Plugins.TryGetValue(requestingPluginName, out requestingPluginContainer);
                }

                Logger.Debug("Attempting to resolve assembly {assemblyName} from {requestingAssemblyPath}", assemblyName.Name, requestingAssemblyPath);

                IEnumerable<string> allSearchDirectories = requestingAssemblyPath == null
                    ? Array.Empty<string>()
                    : new[]
                    {
                        requestingAssemblyPath
                    };

                if (requestingPluginContainer != null)
                {
                    allSearchDirectories = allSearchDirectories.Concat(requestingPluginContainer.Info.Dependencies.Select(dep => Path.Combine("JKMP", "Plugins", dep.Key)));
                }

                allSearchDirectories = allSearchDirectories.Concat(SearchDirectories);

                foreach (string directoryPath in allSearchDirectories)
                {
                    Assembly? assembly = SearchDirectory(assemblyName, directoryPath);

                    if (assembly != null)
                        return assembly;
                    
                    if (requestingPluginName != null)
                    {
                        // If a plugin is requesting an assembly, also check subfolder "Dependencies"
                        string depsPath = Path.Combine(directoryPath, "Dependencies");

                        assembly = SearchDirectory(assemblyName, depsPath, searchSubPaths: true);

                        if (assembly != null)
                            return assembly;
                    }
                }

                Logger.Error("Failed to resolve assembly: {assemblyName}", assemblyName.Name);
                return null;
            };
        }

        private static Assembly? SearchDirectory(AssemblyName assemblyName, string directoryPath, bool searchSubPaths = false)
        {
            if (!Directory.Exists(directoryPath))
                return null;
            
            foreach (string filePath in Directory.GetFiles(directoryPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                if (fileName.Equals(assemblyName.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    Logger.Debug("Found assembly: {filePath}", filePath);
                    return Assembly.LoadFile(Path.GetFullPath(filePath));
                }
            }

            if (searchSubPaths)
            {
                foreach (string subPath in Directory.GetDirectories(directoryPath))
                {
                    Assembly? assembly = SearchDirectory(assemblyName, subPath, searchSubPaths: true);

                    if (assembly != null)
                        return assembly;
                }
            }

            return null;
        }
    }
}