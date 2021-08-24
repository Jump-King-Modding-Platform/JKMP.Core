using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JKMP.Core.Logging;
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
            "LanguageJK.XmlSerializers"
        };

        private static readonly ILogger Logger = LogManager.CreateLogger(typeof(AssemblyManager));

        public static void SetupAssemblyResolving(AppDomain appDomain)
        {
            appDomain.AssemblyResolve += (sender, args) =>
            {
                Assembly requestingAssembly = args.RequestingAssembly ?? Assembly.GetExecutingAssembly();
                
                var assemblyName = new AssemblyName(args.Name);

                if (IgnoredAssemblies.Contains(assemblyName.Name))
                    return null;
                
                string? requestingAssemblyPath = string.IsNullOrEmpty(requestingAssembly.Location) ? null : Path.GetDirectoryName(requestingAssembly.Location)!;
                Logger.Debug("Attempting to resolve assembly {assemblyName} from {requestingAssemblyPath}", assemblyName.Name, requestingAssemblyPath);

                IEnumerable<string> allSearchDirectories = requestingAssemblyPath == null ? Array.Empty<string>() : new[] { requestingAssemblyPath };
                allSearchDirectories = allSearchDirectories.Concat(SearchDirectories);
                
                foreach (string directoryPath in allSearchDirectories)
                {
                    Assembly? assembly = SearchDirectory(assemblyName, directoryPath);

                    if (assembly != null)
                        return assembly;
                }

                Logger.Error("Failed to resolve assembly: {assemblyName}", assemblyName.Name);
                return null;
            };
        }

        private static Assembly? SearchDirectory(AssemblyName assemblyName, string directoryPath)
        {
            foreach (string filePath in Directory.GetFiles(directoryPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                if (fileName.Equals(assemblyName.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    Logger.Debug("Found assembly: {filePath}", filePath);
                    return Assembly.LoadFrom(filePath);
                }
            }

            foreach (string subPath in Directory.GetDirectories(directoryPath))
            {
                return SearchDirectory(assemblyName, subPath);
            }

            return null;
        }
    }
}