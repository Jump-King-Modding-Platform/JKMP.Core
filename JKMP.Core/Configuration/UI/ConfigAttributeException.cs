using System;

namespace JKMP.Core.Configuration.UI
{
    /// <summary>
    /// An exception that is raised when an error occurs when creating a config menu using reflection.
    /// </summary>
    public class ConfigAttributeException : Exception
    {
        /// <inheritdoc />
        public ConfigAttributeException()
        {
        }

        /// <inheritdoc />
        public ConfigAttributeException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public ConfigAttributeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}