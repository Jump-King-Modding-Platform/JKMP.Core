using System;
using System.IO;
using JKMP.Core.Configuration.Attributes;
using JKMP.Core.Configuration.UI;
using JKMP.Core.Logging;
using JKMP.Core.Plugins;
using Newtonsoft.Json;
using Serilog;

namespace JKMP.Core.Configuration
{
    /// <summary>
    /// Handles loading and saving of configurations for plugins.
    /// </summary>
    public class PluginConfigs
    {
        /// <summary>
        /// Gets the json serializer settings to use when serializing and deserializing the config files.
        /// </summary>
        public JsonSerializerSettings? JsonSerializerSettings { get; internal set; }
        private readonly Plugin owner;

        private static readonly ILogger Logger = LogManager.CreateLogger<PluginConfigs>();
        
        internal PluginConfigs(Plugin owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Loads a configuration file from a file matching the sourceName. If it doesn't exist, it will be created and saved with default values.
        /// </summary>
        /// <param name="sourceName">The name of the file to load. It will be appended with .json if it doesn't already end with it.</param>
        /// <param name="saveToDisk">If true the config will be saved to disk if it didn't exist.</param>
        /// <param name="syncChanges">If true the config will be saved to disk after being loaded to sync any new members that may have been added or removed in a new version.</param>
        /// <exception cref="JsonException">Thrown if any deserialization or serialization issues occur while loading or saving the file.</exception>
        /// <exception cref="IOException">Thrown when an error occurs while loading or saving the file to disk.</exception>
        public T LoadConfig<T>(string sourceName, bool saveToDisk = true, bool syncChanges = true) where T : class, new()
        {
            if (!sourceName.EndsWith(".json"))
                sourceName += ".json";
            
            T? config;
            var configFilePath = Path.Combine(owner.Container.ConfigRoot, sourceName);

            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                try
                {
                    config = JsonConvert.DeserializeObject<T>(json) ?? throw new JsonException($"Deserialized object is null");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Config file {sourceName} for plugin {pluginName} is not valid", owner.Id);
                    throw;
                }
                
                if (syncChanges)
                    SaveConfig(config, sourceName);
            }
            else
            {
                config = new T();

                if (saveToDisk)
                    SaveConfig(config, sourceName);
            }

            return config;
        }

        /// <summary>
        /// Saves a configuration file to a file matching the sourceName.
        /// </summary>
        /// <param name="config">The config object to save.</param>
        /// <param name="sourceName">The name of the file to save to. It will be appended with .json if it doesn't already end with it.</param>
        /// <exception cref="JsonException">Thrown if an issue occurs during serialization.</exception>
        /// <exception cref="IOException">Thrown if an issue occurs when saving the file to disk.</exception>
        public void SaveConfig<T>(T config, string sourceName) where T : class
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            if (!sourceName.EndsWith(".json"))
                sourceName += ".json";
            
            string configFilePath = Path.Combine(owner.Container.ConfigRoot, sourceName);
            string configDirPath = Path.GetDirectoryName(configFilePath)!;

            try
            {
                var json = JsonConvert.SerializeObject(config, JsonSerializerSettings);
                
                if (!Directory.Exists(configDirPath))
                    Directory.CreateDirectory(configDirPath);
                
                File.WriteAllText(configFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occured while trying to save the config file");
                throw;
            }
        }

        /// <summary>
        /// Adds a menu in the settings that will let the user modify the loaded config in-game.
        /// Values are saved automatically when they are changed.
        /// The properties are parsed by the following attributes:
        /// <para>
        /// <see cref="SettingsOptionAttribute"/> (and any class that inherits it)
        /// To implement your own settings option attribute, inherit this class and add the <see cref="SettingsOptionCreatorAttribute"/>.
        /// </para>
        /// </summary>
        /// <param name="name">The name of the sub-menu.</param>
        /// <param name="sourceName">The source filename of the config.</param>
        /// <typeparam name="T">The type that contains the values of the config.</typeparam>
        /// <returns>Returns the config menu. With it you can access the config or subscribe to an event that is invoked when any value changes.</returns>
        public IConfigMenu<T> CreateConfigMenu<T>(string name, string sourceName) where T : class, new()
        {
            return CreateConfigMenu<ReflectedConfigMenu<T>, T>(name, sourceName, menuName => new ReflectedConfigMenu<T>(owner, menuName));
        }
        
        /// <summary>
        /// Adds a custom menu in the settings that will let the user modify the loader config in-game.
        /// </summary>
        /// <param name="name">The name of the menu.</param>
        /// <param name="sourceName">The source filename of the config.</param>
        /// <param name="configMenuFactory">The factory func that is invoked to create the config menu. The passed parameters is the </param>
        /// <typeparam name="TConfigMenuType">The type of the menu.</typeparam>
        /// <typeparam name="TConfigType">The config type that this menu will be configuring.</typeparam>
        /// <returns>Returns the config menu. With it you can access the config or subscribe to an event that is invoked when any value changes.</returns>
        public IConfigMenu<TConfigType> CreateConfigMenu<TConfigMenuType, TConfigType>(string name, string sourceName, Func<string, TConfigMenuType> configMenuFactory)
            where TConfigType : class, new() where TConfigMenuType : IConfigMenu<TConfigType>
        {
            var menu = configMenuFactory(sourceName);
            SettingsMenuManager.AddMenu(owner, name, menu);

            return menu;
        }
    }
}