using System;
using System.IO;
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
        /// <exception cref="JsonException">Thrown if any deserialization or serialization issues occur while loading or saving the file.</exception>
        /// <exception cref="IOException">Thrown when an error occurs while loading or saving the file to disk.</exception>
        public T LoadConfig<T>(string sourceName, bool saveToDisk = true) where T : class, new()
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
                    Logger.Error(ex, "Config file {sourceName} for plugin {pluginName} is not valid", owner.Info.Name);
                    throw;
                }
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
    }
}