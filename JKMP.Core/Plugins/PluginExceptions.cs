using System;
using System.IO;

namespace JKMP.Core.Plugins
{
    /// <summary>
    /// The exception that is thrown when an attempt to load a plugin failed due to an unhandled exception.
    /// </summary>
    public class PluginLoadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginLoadException"/> and sets the message.
        /// </summary>
        /// <param name="message"></param>
        public PluginLoadException(string message) : base(message) { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginLoadException"/> and sets the message and inner exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public PluginLoadException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// The exception that is thrown when an attempt was made to load a non existing plugin.
    /// </summary>
    public class PluginNotFoundException : FileNotFoundException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginNotFoundException"/>.
        /// </summary>
        public PluginNotFoundException() { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginNotFoundException"/> and sets the message.
        /// </summary>
        /// <param name="message"></param>
        public PluginNotFoundException(string message) : base(message) { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginNotFoundException"/> and sets the message and file name.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="fileName"></param>
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