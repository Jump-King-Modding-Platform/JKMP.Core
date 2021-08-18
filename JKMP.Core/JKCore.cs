using System;
using HarmonyLib;
using JKMP.Core.Plugins;

namespace JKMP.Core
{
    public sealed class JKCore
    {
        public PluginManager Plugins { get; }
        
        internal JKCore()
        {
            Console.WriteLine("Initializing JKMP!");
            
            Plugins = new();
            Plugins.LoadPlugins();
        }
    }
}