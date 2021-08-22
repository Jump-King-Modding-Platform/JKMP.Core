using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

        public static void SetupAssemblyResolving(AppDomain appDomain)
        {
            appDomain.AssemblyResolve += (sender, args) =>
            {
                Assembly requestingAssembly = args.RequestingAssembly ?? Assembly.GetExecutingAssembly();
                
                var assemblyName = new AssemblyName(args.Name);

                if (IgnoredAssemblies.Contains(assemblyName.Name))
                    return null;
                
                string? requestingAssemblyPath = string.IsNullOrEmpty(requestingAssembly.Location) ? null : Path.GetDirectoryName(requestingAssembly.Location)!;
                Console.WriteLine($"Attempting to resolve assembly {assemblyName.Name} from {requestingAssemblyPath}");

                IEnumerable<string> allSearchDirectories = requestingAssemblyPath == null ? Array.Empty<string>() : new[] { requestingAssemblyPath };
                allSearchDirectories = allSearchDirectories.Concat(SearchDirectories);
                
                foreach (string directoryPath in allSearchDirectories)
                {
                    Assembly? assembly = SearchDirectory(assemblyName, directoryPath);

                    if (assembly != null)
                        return assembly;
                }

                Console.WriteLine($"Failed to resolve assembly: {assemblyName.Name}");
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
                    Console.WriteLine($"Found assembly: {filePath}");
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