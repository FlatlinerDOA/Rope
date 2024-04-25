/*
* Diff Match and Patch
* Copyright 2018 The diff-match-patch Authors.
* https://github.com/google/diff-match-patch
* Copyright 2024 Andrew Chisholm (FlatlinerDOA).
* https://github.com/FlatlinerDOA/Rope
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
* 
*/

namespace Rope.Compare;

using System;
using System.Diagnostics.Contracts;

public static class MatchAlgorithmExtensions
{
    /// <summary>
    /// Initialise the alphabet for the Bitap algorithm.
    /// </summary>
    /// <param name="pattern">The text to encode.</param>
    /// <returns>Bitmask of element indexes.</returns>
    [Pure]
    internal static Dictionary<T, long> MatchAlphabet<T>(this Rope<T> pattern) where T : IEquatable<T>
    {
        var s = new Dictionary<T, long>();
        long i = 0;
        foreach (var c in pattern)
        {
            long value = s.GetValueOrDefault(c) | (1L << (int)(pattern.Length - i - 1));
            s[c] = value;
            i++;
        }

        return s;
    }

    /// <summary>
    /// Compute and return the score for a match with e errors and x location.
    /// </summary>
    /// <param name="pattern">Pattern being sought.</param>
    /// <param name="e">Number of errors in match.</param>
    /// <param name="x">Location of match.</param>
    /// <param name="loc">Expected location of match.</param>
    /// <param name="options">Options for matching.</param>
    /// <returns>Overall score for match (0.0 = good, 1.0 = bad).</returns>
    [Pure]
    internal static double MatchBitapScore<T>(this Rope<T> pattern, long e, long x, long loc, MatchOptions options) where T : IEquatable<T> 
    {
        float accuracy = (float)e / pattern.Length;
        var proximity = Math.Abs(loc - x);
        if (options.MatchDistance == 0)
        {
            // Dodge divide by zero error.
            return proximity == 0 ? accuracy : 1.0;
        }
        return accuracy + (proximity / (float)options.MatchDistance);
    }
}
