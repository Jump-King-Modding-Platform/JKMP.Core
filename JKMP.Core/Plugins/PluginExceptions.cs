using System;
using System.IO;

namespace JKMP.Core.Plugins
{
    /// <summary>
    /// The exception that is thrown when an attempt to load a plugin failed due to an unhandled exception.
    /// </summary>
    public class PluginLoadException : Exception
    {
        public PluginLoadException(string message) : base(message) { }
        public PluginLoadException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// The exception that is thrown when an attempt was made to load a non existing plugin.
    /// </summary>
    public class PluginNotFoundException : FileNotFoundException
    {
        public PluginNotFoundException() { }
        public PluginNotFoundException(string message) : base(message) { }
        public PluginNotFoundException(string message, string fileName) : base(message, fileName) { }
    }

    /// <summary>
    /// The exception that is thrown when a plugin loader could not be loaded.
    /// </summary>
    internal class PluginLoaderException : Exception
    {
        public PluginLoaderException(string message) : base(message) { }
    }
}