using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JKMP.Core.Logging;
using Newtonsoft.Json;
using Serilog;

namespace JKMP.Core.Plugins
{
    public sealed class PluginManager : IEnumerable<PluginContainer>
    {
        private readonly Dictionary<string, PluginContainer> loadedPlugins = new();

        private readonly List<IPluginLoader> loaders = new();

        private static readonly Regex RgxLoaderFileName = new Regex(@"^JKMP\.(?:.*)Loader\.(?<name>.+)\.dll$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly ILogger Logger = LogManager.CreateLogger<PluginManager>();
        
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
                PluginContainer? pluginContainer = null;
                
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

                    if (pluginInfo.OnlyContent)
                    {
                        pluginContainer = new PluginContainer(new ContentPlugin(), pluginInfo, null)
                        {
                            RootDirectory = pluginDirectory,
                            ContentRoot = Path.Combine(pluginDirectory, "Content")
                        };

                        loadedPlugins[pluginDirectory] = pluginContainer;
                        continue;
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

                        Logger.Information(
                            "Loading plugin '{name}' v{version} using {loaderName}",
                            pluginInfo.Name,
                            pluginInfo.Version,
                            loader.GetType().Name
                        );

                        pluginContainer = loader.LoadPlugin(entryFileName, pluginInfo);
                        pluginContainer.RootDirectory = pluginDirectory;
                        pluginContainer.ContentRoot = Path.Combine(pluginDirectory, "Content");
                        
                        loadedPlugins[pluginDirectory] = pluginContainer;

                        Logger.Verbose("Plugin loaded");
                    }
                }
                catch (PluginLoadException ex)
                {
                    Logger.Error(
                        "Could not load plugin {directoryName}: {exceptionMessage}",
                        Path.GetFileNameWithoutExtension(pluginDirectory),
                        ex.Message
                    );

                    if (ex.InnerException != null)
                        Logger.Error(ex.InnerException, string.Empty);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "An unhandled exception was raised while loading the plugin");
                }

                pluginContainer?.Plugin.OnLoaded();
            }

            foreach (var pluginContainer in loadedPlugins.Values)
                pluginContainer.Plugin.Initialize();
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
                Logger.Verbose("Found loader dll: {filePath}", filePath);

                try
                {
                    string? expectedTypeName = RgxLoaderFileName.Match(Path.GetFileName(filePath)).Groups["name"]?.Value;

                    if (expectedTypeName == null)
                        throw new PluginLoaderException("The loader filename is not using a valid format.");

                    expectedTypeName += "PluginLoader";
                    
                    var loaderType = typeof(IPluginLoader);
                    Assembly assembly = Assembly.LoadFile(Path.GetFullPath(filePath));

                    foreach (var type in assembly.ExportedTypes)
                    {
                        if (!loaderType.IsAssignableFrom(type))
                            continue;

                        if (type.IsAbstract)
                            continue;

                        Logger.Verbose("Found loader type: {typeName}", type.Name);

                        if (expectedTypeName != type.Name)
                            throw new PluginLoaderException($"The loader type's name needs to match '{expectedTypeName}'.");

                        IPluginLoader instance = (IPluginLoader)Activator.CreateInstance(type);
                        loaders.Add(instance);
                        Logger.Information("Plugin loader loaded: {typeName}", type.Name);
                        break;
                    }
                }
                catch (PluginLoaderException ex)
                {
                    Logger.Error(ex, "Failed to load plugin loader");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "An unhandled exception was thrown");
                }
            }
        }

        public IEnumerator<PluginContainer> GetEnumerator()
        {
            return loadedPlugins.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}