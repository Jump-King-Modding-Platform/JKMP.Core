using System;

namespace Semver.Ranges
{
    /// <summary>
    /// The exception that is thrown when a range's syntax could not be parsed.
    /// </summary>
    public class RangeParseException : Exception
    {
        /// <inheritdoc />
        public RangeParseException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public RangeParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
