using System;
using System.Collections.Generic;
using System.Linq;

namespace Semver.Ranges.Comparers.Npm
{
    internal class RangeParser
    {
        internal static readonly string[] OrSeparator = { "||" };

        public NpmRange ParseRange(string range, NpmParseOptions options)
        {
            string[] strComps = range.Split(OrSeparator, StringSplitOptions.None);

            if (strComps.All(string.IsNullOrWhiteSpace))
            {
                return new NpmRange(new[]
                {
                    new[]
                    {
                        new NpmComparator(options)
                    }
                });
            }

            var ranges = strComps.Select(strComp =>
            {
                var compsEnumerable = new ComparatorParser().ParseComparators(strComp.Trim(), options);
                List<NpmComparator> comps = new List<NpmComparator>();

                foreach (NpmComparator comp in compsEnumerable)
                {
                    // Only add unique comparators
                    if (!comps.Contains(comp))
                        comps.Add(comp);
                }

                return comps;
            }).ToList();

            // Simplify ranges if they contain all inclusive or all exclusive ranges
            NpmComparator? allInclusiveComp = null; // *
            List<NpmComparator>? allInclusiveContainer = null; // Container for allInclusive
            NpmComparator? allExclusiveComp = null; // <0.0.0-0
            List<NpmComparator>? allExclusiveContainer = null;

            foreach (var comps in ranges)
            {
                // If any range is impossible is satisfy, remove every other range in set
                foreach (var comp in comps)
                {
                    if (allExclusiveComp == null && !comp.AnyVersion && comp.Operator == ComparatorOp.LessThan && comp.Version!.Equals(ComparatorParser.ZeroVersionWithPrerelease))
                    {
                        allExclusiveComp = comp;
                        allExclusiveContainer = comps;

                        if (allInclusiveComp != null)
                            break;
                    }
                    else if (allInclusiveComp == null && comp.AnyVersion || (comp.Operator == ComparatorOp.GreaterThan || comp.Operator == ComparatorOp.GreaterThanOrEqualTo) &&
                             comp.Version!.Equals(ComparatorParser.ZeroVersionWithPrerelease))
                    {
                        allInclusiveComp = comp;
                        allInclusiveContainer = comps;

                        if (allExclusiveComp != null)
                            break;
                    }
                }

                if (allExclusiveComp != null && allInclusiveComp != null)
                    break;
            }

            if (allInclusiveComp != null) // If a range has both * and <0.0.0-0 then prioritize the *
            {
                // Remove all other comparators without allocating more memory
                allInclusiveContainer!.Clear();
                allInclusiveContainer.Add(allInclusiveComp);

                ranges.Clear();
                ranges.Add(allInclusiveContainer);
            }
            else if (allExclusiveComp != null)
            {
                // Remove all other comparators without allocating more memory
                allExclusiveContainer!.Clear();
                allExclusiveContainer.Add(allExclusiveComp);

                ranges.Clear();
                ranges.Add(allExclusiveContainer);
            }

            var comparators = ranges.Select(comps => comps.ToArray()).ToArray();
            foreach (var comps in comparators)
            {
                foreach (var comp in comps)
                {
                    comp.SetRangeComparators(comps);
                }
            }

            return new NpmRange(comparators);
        }
    }
}