using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JKMP.Core.Plugins;

namespace JKMP.Core.Loader.NetFramework
{
    // ReSharper disable once UnusedType.Global
    public class NetFrameworkPluginLoader : IPluginLoader
    {
        public ICollection<string> SupportedExtensions { get; } = new[] { ".dll" };
        
        public PluginContainer LoadPlugin(string filePath, PluginInfo info)
        {
            Console.WriteLine($"Loading plugin: {filePath}");

            Assembly assembly = Assembly.LoadFrom(filePath);
            string pluginDirectory = Path.GetDirectoryName(filePath)!;
            string pluginName = Path.GetFileNameWithoutExtension(pluginDirectory);
            string typeName = $"JKMP.Plugin.{pluginName}.{pluginName}Plugin";

            Type pluginType = assembly.GetType(typeName, true);
            Plugin instance = (Plugin)Activator.CreateInstance(pluginType);

            return new(instance, info, this);
        }
    }
}