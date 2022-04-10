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
using Semver;
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
            var pluginsToLoad = GetPluginsToLoad();

            foreach ((PluginInfo pluginInfo, string pluginDirectory) in pluginsToLoad)
            {
                PluginContainer? pluginContainer = null;
                
                try
                {
                    string uniqueId = new DirectoryInfo(pluginDirectory).Name;

                    if (pluginInfo.OnlyContent)
                    {
                        pluginContainer = new PluginContainer(new ContentPlugin(), pluginInfo, null)
                        {
                            RootDirectory = pluginDirectory,
                            ContentRoot = Path.Combine(pluginDirectory, "Content")
                        };

                        pluginContainer.Plugin.Container = pluginContainer;

                        loadedPlugins[uniqueId] = pluginContainer;
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
                    pluginContainer.Plugin.Id = uniqueId;
                    pluginContainer.Plugin.Container = pluginContainer;
                    pluginContainer.Plugin.Configs = new(pluginContainer.Plugin);
                    pluginContainer.Plugin.Configs.JsonSerializerSettings ??= CreateDefaultJsonSerializerSettings();
                    pluginContainer.Plugin.Input = new(pluginContainer.Plugin);

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
                        Logger.Error(ex.InnerException, "");
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

        private List<(PluginInfo info, string directoryPath)> GetPluginsToLoad()
        {
            string pluginsDirectory = Path.Combine("JKMP", "Plugins");
            Directory.CreateDirectory(pluginsDirectory);
            var plugins = new Dictionary<string, (PluginInfo, string)>();

            foreach (string path in Directory.GetDirectories(pluginsDirectory))
            {
                string id = new DirectoryInfo(path).Name;
                var pluginInfo = LoadPluginInfo(path);
                plugins.Add(id, (pluginInfo, path));
            }

            Dictionary<string, (PluginInfo, string)> orderedPlugins = new(plugins.Count);

            // Order plugins by their dependencies
            foreach (var kv in plugins)
            {
                string id = kv.Key;
                var (info, path) = kv.Value;
                AddPlugin(info, path, orderedPlugins, plugins);
            }

            return orderedPlugins.Values.ToList();
        }

        private bool AddPlugin(PluginInfo info, string path, Dictionary<string, (PluginInfo, string)> orderedPlugins, Dictionary<string, (PluginInfo, string)> allPlugins)
        {
            string id = new DirectoryInfo(path).Name;

            // Check if it was already loaded from a previous plugin depending on it
            if (orderedPlugins.ContainsKey(id))
                return true;
            
            if (info.Dependencies.Count == 0)
            {
                orderedPlugins.Add(id, (info, path));
                return true;
            }

            bool CompareVersions(string id, SemVersion target, SemVersion current)
            {
                if (!IsVersionCompatible(target, current))
                {
                    Logger.Error(
                        "Version mismatch found for dependency '{dependencyPlugin}@{version}' for plugin {pluginName}, installed version is {installedVersion}",
                        id,
                        current,
                        id,
                        target
                    );

                    return false;
                }

                return true;
            }

            foreach (var dependency in info.Dependencies)
            {
                string dependencyId = dependency.Key;
                SemVersion version;

                try
                {
                    version = SemVersion.Parse(dependency.Value, SemVersionStyles.Any);
                }
                catch (Exception e)
                {
                    Logger.Error(
                        "Could not parse version for dependency '{dependencyPlugin}@{version}' for plugin '{pluginName}'",
                        dependencyId,
                        dependency.Value,
                        id
                    );
                    
                    return false;
                }

                if (!orderedPlugins.ContainsKey(dependencyId))
                {
                    if (!allPlugins.ContainsKey(dependencyId))
                    {
                        Logger.Error(
                            "Could not find dependant plugin '{dependencyPlugin}@{version}' for plugin '{pluginName}'",
                            dependencyId,
                            dependency.Value,
                            id
                        );
                        
                        return false;
                    }

                    var tuple = allPlugins[dependencyId];

                    if (tuple.Item1.Dependencies.ContainsKey(id))
                    {
                        Logger.Error(
                            "Circular dependency detected between '{dependencyPlugin}' and '{pluginName}'",
                            dependencyId,
                            id
                        );

                        return false;
                    }

                    if (!CompareVersions(dependencyId, tuple.Item1.Version!, version))
                        return false;

                    if (!AddPlugin(tuple.Item1, tuple.Item2, orderedPlugins, allPlugins))
                        return false;
                }
                else
                {
                    var tuple = allPlugins[dependencyId];

                    if (!CompareVersions(dependencyId, tuple.Item1.Version!, version))
                        return false;
                }
            }
            
            orderedPlugins.Add(id, (info, path));
            return true;
        }

        private bool IsVersionCompatible(SemVersion? target, SemVersion current)
        {
            // todo: implement
            return true;
        }

        private PluginInfo LoadPluginInfo(string pluginPath)
        {
            string pluginMetaDataPath = Path.Combine(pluginPath, "plugin.json");

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

            return pluginInfo;
        }

        private string? FindPluginEntryFile(string pluginDirectory)
        {
            string nameOfPlugin = Path.GetFileNameWithoutExtension(pluginDirectory);
            string filter = $"JKMP.Plugin.{nameOfPlugin}.*";
            string[] matchingFiles = Directory.GetFiles(pluginDirectory, filter);

            if (matchingFiles.Length == 0)
                return null;

            if (matchingFiles.Length > 1)
                throw new PluginLoadException($"There are multiple files matching the JKMP.Plugin.{nameOfPlugin}.* pattern in the plugin's directory");

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