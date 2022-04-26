using System;

namespace Semver.Ranges.Comparers.Npm
{
    /// <summary>
    /// The options to use when parsing a range with npm syntax.
    /// </summary>
    public readonly struct NpmParseOptions : IEquatable<NpmParseOptions>
    {
        /// <summary>
        /// Gets if non-explicitly selected prerelease versions should be included.
        /// </summary>
        public readonly bool IncludePreRelease;
        
        private readonly string stringValue;

        /// <param name="includePreRelease">True if non-explicitly selected prerelease versions should be included.</param>
        public NpmParseOptions(bool includePreRelease = false)
        {
            IncludePreRelease = includePreRelease;
            stringValue = $"{{ IncludePreRelease: {IncludePreRelease} }}";
        }

        /// <inheritdoc />
        public override string ToString() => stringValue;

        /// <summary>
        /// Checks if the options of this instance are identical to 'other'.
        /// </summary>
        /// <returns>Returns true if the options in both instances are the same.</returns>
        public bool Equals(NpmParseOptions other)
        {
            return IncludePreRelease == other.IncludePreRelease;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is NpmParseOptions other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return IncludePreRelease.GetHashCode();
        }
    }
}
