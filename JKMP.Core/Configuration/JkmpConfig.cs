using System.Collections.Generic;

namespace JKMP.Core.Configuration
{
    internal class JkmpConfig
    {
        public bool FirstStartup { get; set; } = true;

        public List<string> PluginLoadOrder { get; set; } = new();
    }
}