using System.Collections.Generic;
using Newtonsoft.Json;
using Semver;

namespace JKMP.Core.Plugins
{
    /// <summary>
    /// Contains metadata about a plugin, usually loaded from a plugin's manifest file 'plugin.json'.
    /// </summary>
    public sealed class PluginInfo
    {
        /// <summary>
        /// Gets the authors of this plugin.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ICollection<string>? Authors { get; set; }
        
        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string? Name { get; set; }
        
        /// <summary>
        /// Gets the description of what this plugin does.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string? Description { get; set; }
        
        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public SemVersion? Version { get; set; }
        
        /// <summary>
        /// Gets whether or not this plugin only contains content and no additional code.
        /// </summary>
        public bool OnlyContent { get; set; }

        /// <summary>
        /// Gets the dependencies of this plugin. The key is the plugin name and the value is the range of compatible versions.
        /// </summary>
        public Dictionary<string, string> Dependencies { get; } = new();
    }
}