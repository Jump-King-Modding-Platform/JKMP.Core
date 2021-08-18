using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace JKMP.Core.Plugins
{
    public sealed class PluginManager
    {
        private readonly Dictionary<string, PluginContainer> loadedPlugins = new();

        private readonly List<IPluginLoader> loaders = new();

        private static readonly Regex RgxLoaderFileName = new Regex(@"^JKMP\.(?:.*)Loader\.(?<name>.+)\.dll$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        internal PluginManager()
        {
            LoadPluginLoaders();
        }

        internal void LoadPlugins()
        {
            string pluginsDirectory = Path.Combine("JKMP", "Plugins");
            Directory.CreateDirectory(pluginsDirectory);

            foreach (string pluginDirectory in Directory.GetDirectories(pluginsDirectory))
            {
                try
                {
                    string pluginMetaDataPath = Path.Combine(pluginDirectory, "plugin.json");

                    if (!File.Exists(pluginMetaDataPath))
                    {
                        throw new PluginLoadException("Could not find plugin.json");
                    }

                    PluginInfo pluginInfo;

                    try
                    {
                        string jsonContents = File.ReadAllText(pluginMetaDataPath);
                        pluginInfo = JsonConvert.DeserializeObject<PluginInfo>(jsonContents) ?? throw new JsonException("Deserialized plugin.json is null");
                    }
                    catch (JsonException ex)
                    {
                        throw new PluginLoadException("plugin.json is not formatted correctly", ex);
                    }

                    string? entryFileName = FindPluginEntryFile(pluginDirectory);

                    if (entryFileName == null)
                    {
                        throw new PluginLoadException($"Could not find main entry file.");
                    }
                    else
                    {
                        string pluginExtension = Path.GetExtension(entryFileName);
                        IPluginLoader? loader = FindLoaderByExtension(pluginExtension);

                        if (loader == null)
                            throw new PluginLoadException($"Could not find a loader than can load '{pluginExtension}' plugins.");

                        Console.WriteLine($"Loading plugin '{pluginInfo.Name}' v{pluginInfo.Version} using {loader.GetType().Name}");

                        PluginContainer pluginContainer = loader.LoadPlugin(entryFileName, pluginInfo);
                        loadedPlugins[pluginDirectory] = pluginContainer;

                        Console.WriteLine("Plugin loaded");
                    }
                }
                catch (PluginLoadException ex)
                {
                    Console.WriteLine($"Could not load plugin {Path.GetFileNameWithoutExtension(pluginDirectory)}: {ex.Message}");

                    if (ex.InnerException != null)
                        Console.WriteLine(ex.InnerException);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unhandled exception was raised while loading the plugin:\n{ex}");
                }
            }
        }

        private string? FindPluginEntryFile(string pluginDirectory)
        {
            string nameOfPlugin = Path.GetFileNameWithoutExtension(pluginDirectory);
            string filter = $"JKMP.Plugin.{nameOfPlugin}.*";
            string[] matchingFiles = Directory.GetFiles(pluginDirectory, filter);

            if (matchingFiles.Length == 0)
                return null;

            if (matchingFiles.Length > 1)
                throw new PluginLoadException($"There are multiple files matching the JKMP.Plugin.*.* pattern in the plugin's directory");

            return matchingFiles.First();
        }

        private IPluginLoader? FindLoaderByExtension(string fileExtension)
        {
            foreach (IPluginLoader loader in loaders)
            {
                if (loader.SupportedExtensions.Contains(fileExtension))
                    return loader;
            }

            return null;
        }

        private void LoadPluginLoaders()
        {
            string loadersDirectory = Path.Combine("JKMP", "Loaders");
            Directory.CreateDirectory(loadersDirectory);

            foreach (var filePath in Directory.GetFiles(loadersDirectory, "JKMP.*Loader.*.dll", SearchOption.AllDirectories))
            {
                Console.WriteLine($"Found loader dll: {filePath}");

                try
                {
                    string? expectedTypeName = RgxLoaderFileName.Match(Path.GetFileName(filePath)).Groups["name"]?.Value;

                    if (expectedTypeName == null)
                        throw new PluginLoaderException("The loader filename is not using a valid format.");

                    expectedTypeName += "PluginLoader";
                    
                    var loaderType = typeof(IPluginLoader);
                    Assembly assembly = Assembly.LoadFrom(filePath);

                    foreach (var type in assembly.ExportedTypes)
                    {
                        if (!loaderType.IsAssignableFrom(type))
                            continue;

                        if (type.IsAbstract)
                            continue;

                        Console.WriteLine($"Found loader type: {type.Name}");

                        if (expectedTypeName != type.Name)
                            throw new PluginLoaderException($"The loader type's name needs to match '{expectedTypeName}'.");

                        IPluginLoader instance = (IPluginLoader)Activator.CreateInstance(type);
                        loaders.Add(instance);
                        Console.WriteLine($"Plugin loader loaded: {type.Name}");
                        break;
                    }
                }
                catch (PluginLoaderException ex)
                {
                    Console.WriteLine($"Failed to load: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unhandled exception was thrown:\n{ex}");
                }
            }
        }
    }
}