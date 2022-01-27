using System;

namespace JKMP.Core.Configuration.UI
{
    public class ConfigAttributeException : Exception
    {
        public ConfigAttributeException()
        {
        }
        
        public ConfigAttributeException(string message) : base(message)
        {
        }

        public ConfigAttributeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}