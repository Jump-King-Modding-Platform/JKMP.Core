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
using Semver.Ranges;
using Semver.Ranges.Comparers.Npm;
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
        /// Gets the load order of plugins. The first plugin in this list will be the first one initialized, etc.
        /// This is loaded from a json file on first access and cached for future uses.
        /// </summary>
        public static IList<string> PluginLoadOrder => pluginLoadOrder ??= LoadPluginLoadOrder();

        private static IList<string> LoadPluginLoadOrder()
        {
            string filePath = Path.Combine("JKMP", "PluginLoadOrder.json");

            if (!File.Exists(filePath))
                return new List<string>();

            try
            {
                return JsonConvert.DeserializeObject<IList<string>>(File.ReadAllText(filePath)) ?? new List<string>();
            }
            catch (JsonException ex)
            {
                Logger.Warning(ex, "Plugin load order file is invalid");
                return new List<string>();
            }
        }

        internal static void SavePluginLoadOrder()
        {
            if (pluginLoadOrder == null)
                throw new InvalidOperationException("Plugin load order list is null (should not happen)");

            File.WriteAllText(Path.Combine("JKMP", "PluginLoadOrder.json"), JsonConvert.SerializeObject(pluginLoadOrder.Distinct(), Formatting.Indented));
        }

        /// <summary>
        /// Gets the number of loaded plugins.
        /// </summary>
        public int Count => pluginsDict.Count;
        
        private readonly Dictionary<string, PluginContainer> pluginsDict = new();
        private readonly List<PluginContainer> pluginsList = new(); // used for ordered (by load order) enumeration

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

        private static IList<string>? pluginLoadOrder;

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
            
            // Save the load order
            if (pluginsToLoad.Count > 0)
            {
                foreach (var (_, _, pluginId) in pluginsToLoad)
                {
                    if (!PluginLoadOrder.Contains(pluginId))
                    {
                        PluginLoadOrder.Add(pluginId);
                    }
                }
                
                SavePluginLoadOrder();
            }

            foreach ((PluginInfo pluginInfo, string pluginDirectory, string uniqueId) in pluginsToLoad)
            {
                PluginContainer? pluginContainer = null;

                {
                    bool canLoad = true;
                    
                    // Check that all dependant plugins have been loaded
                    foreach (string depPluginId in pluginInfo.Dependencies.Keys)
                    {
                        if (!ContainsKey(depPluginId))
                        {
                            Logger.Error("Can not load plugin {PluginId} because it depends on {DependantPluginId} which is not loaded", uniqueId, depPluginId);
                            canLoad = false;
                        }
                    }

                    if (!canLoad)
                        continue;
                }

                try
                {
                    if (pluginInfo.OnlyContent)
                    {
                        pluginContainer = new PluginContainer(new ContentPlugin(), pluginInfo, null)
                        {
                            RootDirectory = pluginDirectory,
                            ContentRoot = Path.Combine(pluginDirectory, "Content")
                        };

                        pluginContainer.Plugin.Container = pluginContainer;

                        pluginsDict[uniqueId.ToLowerInvariant()] = pluginContainer;
                        pluginsList.Add(pluginContainer);
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
                        uniqueId,
                        pluginInfo.Version,
                        loader.GetType().Name
                    );

                    pluginContainer = loader.LoadPlugin(entryFileName, pluginInfo);
                    pluginContainer.RootDirectory = pluginDirectory;
                    pluginContainer.ContentRoot = Path.Combine(pluginDirectory, "Content");
                    pluginContainer.ConfigRoot = Path.Combine("JKMP", "Configs", uniqueId);
                    pluginContainer.Plugin.Id = uniqueId;
                    pluginContainer.Plugin.Container = pluginContainer;
                    pluginContainer.Plugin.Configs = new(pluginContainer.Plugin);
                    pluginContainer.Plugin.Configs.JsonSerializerSettings ??= CreateDefaultJsonSerializerSettings();
                    pluginContainer.Plugin.Input = new(pluginContainer.Plugin);

                    pluginsDict[uniqueId.ToLowerInvariant()] = pluginContainer;
                    pluginsList.Add(pluginContainer);

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

            foreach (var pluginContainer in pluginsList)
            {
                pluginContainer.Plugin.CreateInputActions();
                pluginContainer.Plugin.Input.FinalizeActions();
            }

            foreach (var pluginContainer in pluginsList)
                pluginContainer.Plugin.Initialize();
        }

        private List<(PluginInfo info, string directoryPath, string pluginId)> GetPluginsToLoad()
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

            List<(PluginInfo info, string path, string id)> orderedPlugins = new(plugins.Count);

            // Order plugins by load order (if possible) then by their dependencies
            int GetLoadOrderIndex(string pluginId)
            {
                var index = PluginLoadOrder.IndexOf(pluginId);
                return index == -1 ? int.MaxValue : index; // If the plugin is not in the load order, it will be loaded after all the other plugins
            }

            foreach (var kv in plugins.OrderBy(kv => GetLoadOrderIndex(kv.Key)))
            {
                var (info, path) = kv.Value;
                AddPlugin(info, path, orderedPlugins, plugins);
            }

            return orderedPlugins;
        }

        private bool AddPlugin(PluginInfo info, string path, List<(PluginInfo info, string path, string id)> orderedPlugins, Dictionary<string, (PluginInfo, string)> allPlugins)
        {
            string id = new DirectoryInfo(path).Name;

            // Check if it was already loaded from a previous plugin depending on it
            if (orderedPlugins.Any(t => t.id == id))
                return true;
            
            if (info.Dependencies.Count == 0)
            {
                orderedPlugins.Add((info, path, id));
                return true;
            }

            bool CompareVersions(string depId, NpmRange range, SemVersion current)
            {
                if (!range.Includes(current))
                {
                    Logger.Error(
                        "Version mismatch found for dependency '{dependencyPlugin}@{versionRange}' for plugin {pluginName}, installed version is {installedVersion}",
                        depId,
                        range,
                        id,
                        current
                    );

                    return false;
                }

                return true;
            }

            foreach (var dependency in info.Dependencies)
            {
                string dependencyId = dependency.Key;
                NpmRange range;

                try
                {
                    range = NpmRange.Parse(dependency.Value);
                }
                catch (RangeParseException ex)
                {
                    Logger.Error(
                        "Could not parse version for dependency '{dependencyPlugin}@{version}' for plugin '{pluginName}'",
                        dependencyId,
                        dependency.Value,
                        id
                    );
                    
                    return false;
                }

                if (orderedPlugins.All(t => t.id != dependencyId))
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

                    if (!CompareVersions(dependencyId, range, tuple.Item1.Version!))
                        return false;

                    if (!range.Includes(tuple.Item1.Version!))
                        return false;

                    if (!AddPlugin(tuple.Item1, tuple.Item2, orderedPlugins, allPlugins))
                        return false;
                }
                else
                {
                    var tuple = allPlugins[dependencyId];

                    if (!CompareVersions(dependencyId, range, tuple.Item1.Version!))
                        return false;
                }
            }

            orderedPlugins.Add((info, path, id));
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

        /// <summary>
        /// Gets an enumerator that iterates through the loaded plugins. The key is the plugin's id and the value is the plugin's container.
        /// The order of the plugins is not guaranteed to be the same as the load order.
        /// </summary>
        /// <returns>An enumerator that iterates through the loaded plugins. The key is the plugin's id and the value is the plugin's container.</returns>
        IEnumerator<KeyValuePair<string, PluginContainer>> IEnumerable<KeyValuePair<string, PluginContainer>>.GetEnumerator() => pluginsDict.GetEnumerator();

        /// <summary>
        /// Gets an enumerator that iterates through the containers of the loaded plugins.
        /// The order is guaranteed to be the same as the order in which the plugins were loaded.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<PluginContainer> GetEnumerator() => pluginsList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Determines whether the plugin with the specified id is loaded.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key) => pluginsDict.ContainsKey(key.ToLowerInvariant());

        /// <summary>
        /// Determines whether the plugin with the specified name is loaded and returns the plugin container if it is.
        /// </summary>
        /// <param name="key">The unique id of the plugin. It is case-insensitive.</param>
        /// <param name="value">If the method returns true, this value is set to the value of the found plugin container.</param>
        /// <returns>True if the plugin is loaded.</returns>
        public bool TryGetValue(string key, out PluginContainer value) => pluginsDict.TryGetValue(key.ToLowerInvariant(), out value);

        /// <summary>
        /// Gets the plugin container of the plugin with the specified unique id. The name is case-insensitive.
        /// If no plugin is found with the specified id, a <see cref="KeyNotFoundException"/> is thrown.
        /// Use <see cref="TryGetValue"/> to avoid this exception if necessary.
        /// </summary>
        /// <param name="key"></param>
        public PluginContainer this[string key] => pluginsDict[key.ToLowerInvariant()];

        /// <summary>
        /// Gets a collection of all the loaded plugin names.
        /// Note that the order of the names is not guaranteed to be the same as the load order of the plugins.
        /// </summary>
        public IEnumerable<string> Keys => pluginsDict.Keys;

        /// <summary>
        /// Gets a collection of all the loaded plugin containers.
        /// The order of the plugins is guaranteed to be the same as the load order.
        /// </summary>
        public IEnumerable<PluginContainer> Values => pluginsList;
    }
}