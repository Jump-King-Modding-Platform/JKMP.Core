using System;
using System.Linq;
using System.Text;

namespace Semver.Ranges.Comparers.Npm
{
    internal class NpmComparator : IEquatable<NpmComparator>
    {
        public readonly ComparatorOp Operator;
        public readonly SemVersion? Version;
        public readonly bool AnyVersion;

        private readonly NpmParseOptions options;

        private NpmComparator[]? comparators; // The other comparators in the same AND range
        private string? cachedStringValue;

        public NpmComparator(ComparatorOp @operator, SemVersion version, NpmParseOptions options)
        {
            if (@operator == ComparatorOp.ReasonablyClose || @operator == ComparatorOp.CompatibleWith)
                throw new ArgumentException("Invalid operator (ReasonablyClose and CompatibleWith are invalid uses in this context)");
            
            Operator = @operator;
            Version = version;
            this.options = options;
        }

        /// <summary>
        /// Any version will match when using this constructor
        /// </summary>
        public NpmComparator(NpmParseOptions options)
        {
            AnyVersion = true;
            this.options = options;
        }

        internal void SetRangeComparators(NpmComparator[] comparators)
        {
            this.comparators = comparators ?? throw new ArgumentNullException(nameof(comparators));
        }

        public bool Includes(SemVersion version)
        {
            // Only allow prerelease if either
            // a) options include pre-releases
            // b) this version is a prerelease and main versions match (ignoring prerelease)
            // c) another comparator in range is prerelease and matches main version
            if (!AnyVersion)
            {
                if (version.IsPrerelease && !options.IncludePreRelease)
                {
                    if (!Version!.IsPrerelease || !MainVersionEquals(version))
                    {
                        bool anySuccess = false;

                        if (comparators!.Length > 1)
                        {
                            foreach (NpmComparator otherComp in comparators)
                            {
                                if (ReferenceEquals(otherComp, this))
                                    continue;

                                if (!otherComp.AnyVersion && otherComp.Version!.IsPrerelease && otherComp.MainVersionEquals(version))
                                {
                                    anySuccess = true;
                                    break;
                                }
                            }
                        }

                        if (!anySuccess)
                            return false;
                    }
                }
            }

            if (AnyVersion)
                return true;

            int comparison = Compare(version);
            bool result;

            switch (Operator)
            {
                case ComparatorOp.LessThan:
                    result = comparison < 0;
                    break;
                case ComparatorOp.GreaterThan:
                    result = comparison > 0;
                    break;
                case ComparatorOp.LessThanOrEqualTo:
                    result = comparison <= 0;
                    break;
                case ComparatorOp.GreaterThanOrEqualTo:
                    result = comparison >= 0;
                    break;
                case ComparatorOp.Equals:
                    result = comparison == 0;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return result;
        }

        public int Compare(SemVersion version)
        {
            int Clamp(int val)
            {
                if (val < -1)
                    return -1;

                if (val > 1)
                    return 1;

                return val;
            }

            if (AnyVersion)
                return 0;

            int compare = version.Major == Version!.Major ? 0 : Clamp(version.Major - Version.Major);

            if (compare != 0)
                return compare;
            
            compare = version.Minor == Version.Minor ? 0 : Clamp(version.Minor - Version.Minor);

            if (compare != 0)
                return compare;

            compare = version.Patch == Version.Patch ? 0 : Clamp(version.Patch - Version.Patch);

            if (compare != 0)
                return compare;

            if ((version.IsPrerelease && Version.IsPrerelease) || (version.IsPrerelease && options.IncludePreRelease))
                compare = Clamp(ComparePreRelease(version));

            return compare;
        }

        private int ComparePreRelease(SemVersion version)
        {
            var xPrereleaseIdentifiers = version.PrereleaseIdentifiers;
            var yPrereleaseIdentifiers = Version!.PrereleaseIdentifiers;

            // Release are higher precedence than prerelease
            var xIsRelease = xPrereleaseIdentifiers.Count == 0;
            var yIsRelease = yPrereleaseIdentifiers.Count == 0;
            if (xIsRelease && yIsRelease) return 0;
            if (xIsRelease) return 1;
            if (yIsRelease) return -1;

            var minLength = Math.Min(xPrereleaseIdentifiers.Count, yPrereleaseIdentifiers.Count);
            for (int i = 0; i < minLength; i++)
            {
                int comparison = xPrereleaseIdentifiers[i].CompareTo(yPrereleaseIdentifiers[i]);
                if (comparison != 0) return comparison;
            }

            return xPrereleaseIdentifiers.Count.CompareTo(yPrereleaseIdentifiers.Count);
        }

        private bool MainVersionEquals(SemVersion version)
        {
            // Not equal if this comparator is * and version is prerelease but options does not include pre-releases
            if (AnyVersion && version.IsPrerelease && !options.IncludePreRelease)
                return false;

            if (AnyVersion)
                return true;

            if (version.Major != Version!.Major)
                return false;

            if (version.Minor != Version.Minor)
                return false;

            if (version.Patch != Version.Patch)
                return false;

            return true;
        }

        public override string ToString()
        {
            if (cachedStringValue != null)
                return cachedStringValue;

            var builder = new StringBuilder();
            if (AnyVersion)
            {
                builder.Append('*');
            }
            else
            {
                builder.Append(ComparatorParser.OperatorsReverse[Operator]);
                builder.Append(Version);
            }

            cachedStringValue = builder.ToString();
            return cachedStringValue;
        }

        public bool Equals(NpmComparator other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                Operator == other.Operator &&
                AnyVersion == other.AnyVersion &&
                options.Equals(other.options) &&
                (Version == null && other.Version == null || other.Version != null && Version != null && Version.Equals(other.Version));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            if (obj is NpmComparator comp)
                return Equals(comp);

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Operator;
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ AnyVersion.GetHashCode();
                hashCode = (hashCode * 397) ^ options.GetHashCode();
                return hashCode;
            }
        }
    }

    internal enum ComparatorOp
    {
        LessThan,
        GreaterThan,
        LessThanOrEqualTo,
        GreaterThanOrEqualTo,
        Equals,
        CompatibleWith,
        ReasonablyClose
    }
}
