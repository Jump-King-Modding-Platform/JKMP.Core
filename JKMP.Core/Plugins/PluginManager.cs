using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JKMP.Core.Configuration;
using JKMP.Core.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace JKMP.Core.Plugins
{
    /// <summary>
    /// Handles the discovery and initialization of plugin loaders.
    /// It can be enumerated to get all the loaded plugins, or indexed to get a specific one, identified by its unique plugin name (e.g. MyPlugin). The indexer is case-insensitive.
    /// </summary>
    public sealed class PluginManager : IReadOnlyCollection<PluginContainer>, IReadOnlyDictionary<string, PluginContainer>
    {
        /// <summary>
        /// Gets the number of loaded plugins.
        /// </summary>
        public int Count => loadedPlugins.Count;
        
        private readonly Dictionary<string, PluginContainer> loadedPlugins = new();

        private readonly List<IPluginLoader> loaders = new();

        private static readonly Regex RgxLoaderFileName = new Regex(@"^JKMP\.(?:.*)Loader\.(?<name>.+)\.dll$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly ILogger Logger = LogManager.CreateLogger<PluginManager>();

        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            Converters =
            {
                new SemVersionConverter(),
            }
        };
        
        internal PluginManager()
        {
            LoadPluginLoaders();
        }

        internal static JsonSerializerSettings CreateDefaultJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
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
                        pluginInfo = JsonConvert.DeserializeObject<PluginInfo>(jsonContents, SerializerSettings) ?? throw new JsonException("Deserialized plugin.json is null");
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

                        pluginContainer.Plugin.Container = pluginContainer;

                        loadedPlugins[pluginDirectory.ToLowerInvariant()] = pluginContainer;
                        continue;
                    }

                    string? entryFileName = FindPluginEntryFile(pluginDirectory);

                    if (entryFileName == null)
                    {
                        throw new PluginLoadException($"Could not find main entry file.");
                    }

                    string pluginExtension = Path.GetExtension(entryFileName);
                    IPluginLoader? loader = FindLoaderByExtension(pluginExtension);

                    if (loader == null)
                        throw new PluginLoadException($"Could not find a loader that can load '{pluginExtension}' plugins.");

                    Logger.Information(
                        "Loading plugin '{name}' v{version} using {loaderName}",
                        pluginInfo.Name,
                        pluginInfo.Version,
                        loader.GetType().Name
                    );

                    pluginContainer = loader.LoadPlugin(entryFileName, pluginInfo);
                    pluginContainer.RootDirectory = pluginDirectory;
                    pluginContainer.ContentRoot = Path.Combine(pluginDirectory, "Content");
                    pluginContainer.ConfigRoot = Path.Combine("JKMP", "Configs", pluginInfo.Name!);
                    pluginContainer.Plugin.Container = pluginContainer;
                    pluginContainer.Plugin.Configs = new PluginConfigs(pluginContainer.Plugin);
                    pluginContainer.Plugin.Configs.JsonSerializerSettings = CreateDefaultJsonSerializerSettings();
                    pluginContainer.Plugin.Input = new();

                    loadedPlugins[Path.GetFileName(pluginDirectory).ToLowerInvariant()] = pluginContainer;

                    Logger.Verbose("Plugin loaded");
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
            {
                pluginContainer.Plugin.CreateInputActions();
                pluginContainer.Plugin.Input.FinalizeActions();
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

        IEnumerator<KeyValuePair<string, PluginContainer>> IEnumerable<KeyValuePair<string, PluginContainer>>.GetEnumerator() => loadedPlugins.GetEnumerator();

        /// <summary>
        /// Gets an enumerator that iterates through the containers of the loaded plugins.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<PluginContainer> GetEnumerator() => loadedPlugins.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Determines whether the plugin with the specified name is loaded.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key) => loadedPlugins.ContainsKey(key.ToLowerInvariant());

        /// <summary>
        /// Determines whether the plugin with the specified name is loaded and returns the plugin container if it is.
        /// </summary>
        /// <param name="key">The unique name of the plugin. It is case-insensitive.</param>
        /// <param name="value">If the method returns true, this value is set to the value of the found plugin container.</param>
        /// <returns>True if the plugin is loaded.</returns>
        public bool TryGetValue(string key, out PluginContainer value) => loadedPlugins.TryGetValue(key.ToLowerInvariant(), out value);

        /// <summary>
        /// Gets the plugin container of the plugin with the specified unique name. The name is case-insensitive.
        /// </summary>
        /// <param name="key"></param>
        public PluginContainer this[string key] => loadedPlugins[key.ToLowerInvariant()];

        /// <summary>
        /// Gets a collection of all the loaded plugin names.
        /// </summary>
        public IEnumerable<string> Keys => loadedPlugins.Keys;
        
        /// <summary>
        /// Gets a collection of all the loaded plugin containers.
        /// </summary>
        public IEnumerable<PluginContainer> Values => loadedPlugins.Values;
    }
}