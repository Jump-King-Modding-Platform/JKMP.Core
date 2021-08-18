using System.Collections.Generic;
using Newtonsoft.Json;

namespace JKMP.Core.Plugins
{
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
        public string? Version { get; set; }
    }
}