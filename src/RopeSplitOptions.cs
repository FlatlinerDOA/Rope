// Copyright 2024 Andrew Chisholm (https://github.com/FlatlinerDOA)

namespace Rope;

public enum RopeSplitOptions
{
    /// <summary>
    /// Excludes the separator from the results, includes empty results.
    /// </summary>
    None = 0,

    /// <summary>
    /// Excludes the separator and excludes empty results.
    /// </summary>
    RemoveEmpty = 1,

    /// <summary>
    /// Includes the separator at the end of each result (except the last), empty results are not possible.
    /// </summary>
    SplitAfterSeparator = 2,

    /// <summary>
    /// Includes the separator at the start of each result (except the first), empty results are not possible.
    /// </summary>
    SplitBeforeSeparator = 3,
}
