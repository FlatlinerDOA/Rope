/*
* Diff Match and Patch
* Copyright 2018 The diff-match-patch Authors.
* https://github.com/google/diff-match-patch
*
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
    [Pure]
    public static int MatchPattern(this string text, string pattern, int loc, MatchOptions? options) => (int)MatchPattern(text.ToRope(), pattern.ToRope(), loc, options ?? MatchOptions.Default);

    /// <summary>
    /// Locate the best instance of 'pattern' in 'text' near 'loc'.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="pattern">The pattern to search for.</param>
    /// <param name="loc">The location to search around.</param>
    /// <param name="options"></param>
    /// <returns>Best match index or -1.</returns>
    [Pure]
    public static long MatchPattern(this Rope<char> text, Rope<char> pattern, long loc, MatchOptions options)
    {
        // Check for null inputs not needed since null can't be passed in C#.
        loc = Math.Max(0, Math.Min(loc, text.Length));
        if (text == pattern)
        {
            // Shortcut (potentially not guaranteed by the algorithm)
            return 0;
        }
        else if (text.Length == 0)
        {
            // Nothing to match.
            return -1;
        }
        else if (loc + pattern.Length <= text.Length && text.Slice(loc, pattern.Length) == pattern)
        {
            // Perfect match at the perfect spot!  (Includes case of null pattern)
            return loc;
        }
        else
        {
            // Do a fuzzy compare.
            return text.MatchBitap(pattern, loc, options);
        }
    }

    [Pure]
    internal static long MatchBitap(this Rope<char> text, Rope<char> pattern, long loc, MatchOptions options)
    {
        // assert (Match_MaxBits == 0 || pattern.Length <= Match_MaxBits)
        //    : "Pattern too long for this application.";

        // Initialise the alphabet.
        var s = pattern.MatchAlphabet();

        // Highest score beyond which we give up.
        double score_threshold = options.MatchThreshold;
        // Is there a nearby exact match? (speedup)
        var best_loc = text.IndexOf(pattern, loc);
        if (best_loc != -1)
        {
            score_threshold = Math.Min(pattern.MatchBitapScore(0, best_loc, loc, options), score_threshold);
            // What about in the other direction? (speedup)
            best_loc = text.LastIndexOf(pattern, Math.Min(loc + pattern.Length, text.Length));
            if (best_loc != -1)
            {
                score_threshold = Math.Min(pattern.MatchBitapScore(0, best_loc, loc, options), score_threshold);
            }
        }

        // Initialise the bit arrays.
        int matchmask = 1 << (int)(pattern.Length - 1);
        best_loc = -1;

        long bin_min, bin_mid;
        long bin_max = pattern.Length + text.Length;
        // Empty initialization added to appease C# compiler.
        var last_rd = Array.Empty<long>();
        for (var d = 0; d < pattern.Length; d++)
        {
            // Scan for the best match; each iteration allows for one more error.
            // Run a binary search to determine how far from 'loc' we can stray at
            // this error level.
            bin_min = 0;
            bin_mid = bin_max;
            while (bin_min < bin_mid)
            {
                if (pattern.MatchBitapScore(d, loc + bin_mid, loc, options)
                    <= score_threshold)
                {
                    bin_min = bin_mid;
                }
                else
                {
                    bin_max = bin_mid;
                }
                bin_mid = (bin_max - bin_min) / 2 + bin_min;
            }
            // Use the result from this iteration as the maximum for the next.
            bin_max = bin_mid;
            var start = Math.Max(1, loc - bin_mid + 1);
            var finish = Math.Min(loc + bin_mid, text.Length) + pattern.Length;

            var rd = new long[finish + 2];
            rd[finish + 1] = (1 << d) - 1;
            for (var j = finish; j >= start; j--)
            {
                long charMatch;
                if (text.Length <= j - 1 || !s.ContainsKey(text[j - 1]))
                {
                    // Out of range.
                    charMatch = 0;
                }
                else
                {
                    charMatch = s[text[j - 1]];
                }
                if (d == 0)
                {
                    // First pass: exact match.
                    rd[j] = ((rd[j + 1] << 1) | 1) & charMatch;
                }
                else
                {
                    // Subsequent passes: fuzzy match.
                    rd[j] = ((rd[j + 1] << 1) | 1) & charMatch
                        | (((last_rd[j + 1] | last_rd[j]) << 1) | 1) | last_rd[j + 1];
                }
                if ((rd[j] & matchmask) != 0)
                {
                    double score = pattern.MatchBitapScore(d, j - 1, loc, options);
                    // This match will almost certainly be better than any existing
                    // match.  But check anyway.
                    if (score <= score_threshold)
                    {
                        // Told you so.
                        score_threshold = score;
                        best_loc = j - 1;
                        if (best_loc > loc)
                        {
                            // When passing loc, don't exceed our current distance from loc.
                            start = Math.Max(1, 2 * loc - best_loc);
                        }
                        else
                        {
                            // Already passed loc, downhill from here on in.
                            break;
                        }
                    }
                }
            }

            if (pattern.MatchBitapScore(d + 1, loc, loc, options) > score_threshold)
            {
                // No hope for a (better) match at greater error levels.
                break;
            }

            last_rd = rd;
        }
        return best_loc;
    }

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
