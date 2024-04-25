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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

/// <summary>
/// Class containing the diff, match and patch methods.
/// Also Contains the behaviour settings.
/// </summary>
public partial class DiffMatchPatch
{
    public DiffMatchPatch()
    {
        this.DiffOptions = DiffOptions<char>.LineLevel;
        this.PatchOptions = PatchOptions.Default;
    }
    public DiffOptions<char> DiffOptions { get; set; }
    public PatchOptions PatchOptions { get; set; }

    //  DIFF FUNCTIONS

    /// <summary>
    /// Given the original text1, and an encoded string which describes the
    /// operations required to transform text1 into text2, compute the full diff.
    /// </summary>
    /// <param name="text1">Source string for the diff.</param>
    /// <param name="delta">Delta text.</param>
    /// <returns>Array of Diff objects or null if invalid.</returns>
    /// <exception cref="ArgumentException">If invalid input.</exception>
    [Pure]
    public Rope<Diff<char>> DifferencesFromDelta(Rope<char> text1, Rope<char> delta)
    {
        var diffs = Rope<Diff<char>>.Empty;

        // Cursor in text1
        int pointer = 0;
        var tokens = delta.Split('\t');
        foreach (var token in tokens)
        {
            if (token.Length == 0)
            {
                // Blank tokens are ok (from a trailing \t).
                continue;
            }

            // Each token begins with a one character parameter which specifies the
            // operation of this token (delete, insert, equality).
            var param = token.Slice(1);
            switch (token[0])
            {
                case '+':
                    param = param.DiffDecode();
                    diffs = diffs.Add(new Diff<char>(Operation.INSERT, param));
                    break;
                case '-':
                // Fall through.
                case '=':
                    int n;
                    try
                    {
                        n = Convert.ToInt32(param.ToString());
                    }
                    catch (FormatException e)
                    {
                        throw new ArgumentException(("Invalid number in diff_fromDelta: " + param).ToString(), e);
                    }
                    if (n < 0)
                    {
                        throw new ArgumentException(("Negative number in diff_fromDelta: " + param).ToString());
                    }

                    Rope<char> text;
                    try
                    {
                        text = text1.Slice(pointer, n);
                        pointer += n;
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        throw new ArgumentException($"Delta length ({pointer}) larger than source text length ({text1.Length}).", e);
                    }

                    if (token[0] == '=')
                    {
                        diffs = diffs.Add(new Diff<char>(Operation.EQUAL, text));
                    }
                    else
                    {
                        diffs = diffs.Add(new Diff<char>(Operation.DELETE, text));
                    }

                    break;
                default:
                    // Anything else is an error.
                    throw new ArgumentException($"Invalid diff operation in diff_fromDelta: {token[0]}");
            }
        }

        if (pointer != text1.Length)
        {
            throw new ArgumentException("Delta length (" + pointer + ") smaller than source text length (" + text1.Length + ").");
        }

        return diffs;
    }


    //  MATCH FUNCTIONS

    [Pure]
    public int MatchPattern(string text, string pattern, int loc) => (int)MatchPattern(text.ToRope(), pattern.ToRope(), loc, this.PatchOptions);

    /// <summary>
    /// Locate the best instance of 'pattern' in 'text' near 'loc'.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="pattern">The pattern to search for.</param>
    /// <param name="loc">The location to search around.</param>
    /// <param name="options"></param>
    /// <returns>Best match index or -1.</returns>
    [Pure]
    public long MatchPattern(Rope<char> text, Rope<char> pattern, long loc, MatchOptions options)
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
            return MatchBitap(text, pattern, loc, options);
        }
    }

    [Pure]
    protected long MatchBitap(Rope<char> text, Rope<char> pattern, long loc, MatchOptions options)
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



    //  PATCH FUNCTIONS



    /// <summary>
    /// Merge a set of patches onto the text. Returns the patched text, as well
    /// as an array of true/false values indicating which patches were applied.
    /// </summary>
    /// <param name="patches">Array of Patch objects</param>
    /// <param name="text">Old text.</param>
    /// <returns>Value tuple containing the new text and an array of
    /// bool values for whether each patch was applied.</returns>
    [Pure]
    public (string Text, bool[] Applied) ApplyPatches(IEnumerable<Patch<char>> patches, string text)
    {
        var (result, applied) = ApplyPatches(patches.ToRope(), text.ToRope());
        return (result.ToString(), applied);
    }

    public (Rope<char> Text, bool[] Applied) ApplyPatches(Rope<Patch<char>> patches, Rope<char> text) => ApplyPatches(patches, text, this.PatchOptions);

    /// <summary>
    /// Merge a set of patches onto the text. Returns the patched text, as well
    /// as an array of true/false values indicating which patches were applied.
    /// </summary>
    /// <param name="patches">Array of Patch objects</param>
    /// <param name="text">Old text.</param>
    /// <returns>Value tuple containing the new rope and an array of
    /// bool values for whether each patch was applied.</returns>
    [Pure]
    public (Rope<char> Text, bool[] Applied) ApplyPatches(Rope<Patch<char>> patches, Rope<char> text, PatchOptions options)
    {
        if (patches.Count == 0)
        {
            return (text, Array.Empty<bool>());
        }

        (var nullPadding, patches) = this.PatchAddPadding(patches, options);
        text = nullPadding + text + nullPadding;
        patches = this.PatchSplitMaxLength(patches, options);

        long x = 0;

        // delta keeps track of the offset between the expected and actual
        // location of the previous patch.  If there are patches expected at
        // positions 10 and 20, but the first patch was found at 12, delta is 2
        // and the second patch has an effective expected position of 22.
        long delta = 0;
        bool[] results = new bool[patches.Count];
        foreach (var aPatch in patches)
        {
            var expected_loc = aPatch.Start2 + delta;
            var text1 = aPatch.Diffs.ToSource();
            long start_loc;
            long end_loc = -1;
            if (text1.Length > options.MaxLength)
            {
                // patch_splitMax will only provide an oversized pattern
                // in the case of a monster delete.
                start_loc = MatchPattern(text, text1.Slice(0, options.MaxLength), expected_loc, options);
                if (start_loc != -1)
                {
                    end_loc = MatchPattern(
                        text,
                        text1.Slice(text1.Length - options.MaxLength),
                        expected_loc + text1.Length - options.MaxLength,
                        options);
                    if (end_loc == -1 || start_loc >= end_loc)
                    {
                        // Can't find valid trailing context.  Drop this patch.
                        start_loc = -1;
                    }
                }
            }
            else
            {
                start_loc = this.MatchPattern(text, text1, expected_loc, options);
            }

            if (start_loc == -1)
            {
                // No match found.  :(
                results[x] = false;
                // Subtract the delta for this failed patch from subsequent patches.
                delta -= aPatch.Length2 - aPatch.Length1;
            }
            else
            {
                // Found a match.  :)
                results[x] = true;
                delta = start_loc - expected_loc;
                var text2 = Rope<char>.Empty;
                if (end_loc == -1)
                {
                    text2 = text.JavaSubstring(start_loc, Math.Min(start_loc + text1.Length, text.Length));
                }
                else
                {
                    text2 = text.JavaSubstring(start_loc, Math.Min(end_loc + options.MaxLength, text.Length));
                }
                if (text1 == text2)
                {
                    // Perfect match, just shove the Replacement text in.
                    text = text.Slice(0, start_loc) + aPatch.Diffs.ToTarget() + text.Slice(start_loc + text1.Length);
                }
                else
                {
                    // Imperfect match.  Run a diff to get a framework of equivalent
                    // indices.
                    var diffs = text1.Diff(text2, this.DiffOptions with { IsChunkingEnabled = false });
                    if (text1.Length > options.MaxLength && diffs.CalculateEditDistance() / (float)text1.Length > options.DeleteThreshold)
                    {
                        // The end points match, but the content is unacceptably bad.
                        results[x] = false;
                    }
                    else
                    {
                        diffs = diffs.DiffCleanupSemanticLossless(CancellationToken.None);
                        long index1 = 0;
                        foreach (var aDiff in aPatch.Diffs)
                        {
                            if (aDiff.Operation != Operation.EQUAL)
                            {
                                var index2 = diffs.TranslateToTargetIndex(index1);
                                if (aDiff.Operation == Operation.INSERT)
                                {
                                    // Insertion
                                    text = text.InsertRange(start_loc + index2, aDiff.Text);
                                }
                                else if (aDiff.Operation == Operation.DELETE)
                                {
                                    // Deletion
                                    text = text.RemoveRange(start_loc + index2, diffs.TranslateToTargetIndex(index1 + aDiff.Text.Length) - index2);
                                }
                            }

                            if (aDiff.Operation != Operation.DELETE)
                            {
                                index1 += aDiff.Text.Length;
                            }
                        }
                    }
                }
            }

            x++;
        }

        // Strip the padding off.
        text = text.Slice(nullPadding.Length, text.Length - 2 * nullPadding.Length);
        return (text, results);
    }

    /// <summary>
    /// Add some padding on text start and end so that edges can match something.
    /// Intended to be called only from within patch_apply.
    /// <param name="patches">Array of Patch objects.</param>
    /// <param name="options"></param>
    /// <returns>The padding string added to each side.</returns>
    [Pure]
    protected (Rope<char> NullPadding, Rope<Patch<char>> BumpedPatches) PatchAddPadding(IEnumerable<Patch<char>> patches, PatchOptions options)
    {
        short paddingLength = options.Margin;
        var nullPadding = Rope<char>.Empty;
        for (short x = 1; x <= paddingLength; x++)
        {
            nullPadding += (char)x;
        }

        // Bump all the patches forward.
        var bumpedPatches = patches.Select(p => p with { Start1 = p.Start1 + paddingLength, Start2 = p.Start2 + paddingLength }).ToRope();

        // Add some padding on start of first diff.
        var patch = bumpedPatches[0];
        var diffs = patch.Diffs;
        if (diffs.Count == 0 || diffs[0].Operation != Operation.EQUAL)
        {
            // Add nullPadding equality.
            patch = patch with
            {
                Start1 = patch.Start1 - paddingLength,  // Should be 0.
                Start2 = patch.Start2 - paddingLength,  // Should be 0.
                Length1 = patch.Length1 + paddingLength,
                Length2 = patch.Length2 + paddingLength,
                Diffs = diffs.Insert(0, new Diff<char>(Operation.EQUAL, nullPadding))
            };
        }
        else if (paddingLength > diffs[0].Text.Length)
        {
            // Grow first equality.
            var firstDiff = diffs[0];
            var extraLength = paddingLength - firstDiff.Text.Length;

            patch = patch with
            {
                Start1 = patch.Start1 - extraLength,
                Start2 = patch.Start2 - extraLength,
                Length1 = patch.Length1 + extraLength,
                Length2 = patch.Length2 + extraLength,
                Diffs = diffs.SetItem(0, firstDiff.Prepend(nullPadding.Slice(firstDiff.Text.Length)))
            };
        }

        bumpedPatches = bumpedPatches.SetItem(0, patch);

        // Add some padding on end of last diff.
        patch = bumpedPatches[^1];
        diffs = patch.Diffs;
        if (diffs.Count == 0 || diffs.Last().Operation != Operation.EQUAL)
        {
            // Add nullPadding equality.
            patch = patch with
            {
                Length1 = patch.Length1 + paddingLength,
                Length2 = patch.Length2 + paddingLength,
                Diffs = diffs.Add(new Diff<char>(Operation.EQUAL, nullPadding)),
            };
        }
        else if (paddingLength > diffs[^1].Text.Length)
        {
            // Grow last equality.
            var lastDiff = diffs[^1];
            var extraLength = paddingLength - lastDiff.Text.Length;
            patch = patch with
            {
                Length1 = patch.Length1 + extraLength,
                Length2 = patch.Length2 + extraLength,
                Diffs = diffs.SetItem(diffs.Count - 1, lastDiff.Append(nullPadding.Slice(0, extraLength)))
            };
        }

        bumpedPatches = bumpedPatches.SetItem(bumpedPatches.Length - 1,  patch);
        return (nullPadding, bumpedPatches);
    }   

    [Pure]
    public Rope<Patch<char>> PatchSplitMaxLength(IEnumerable<Patch<char>> patches, PatchOptions options)
    {
        var results = patches.ToRope();
        short patch_size = options.MaxLength;
        for (int x = 0; x < results.Count; x++)
        {
            if (results[x].Length1 <= patch_size)
            {
                continue;
            }

            var bigpatch = results[x];

            // Remove the big old patch.
            (_, results) = results.Splice(x--, 1);
            var start1 = bigpatch.Start1;
            var start2 = bigpatch.Start2;
            var precontext = Rope<char>.Empty;
            while (bigpatch.Diffs.Count != 0)
            {
                // Create one of several smaller patches.
                bool empty = true;
                var patch = new Patch<char>()
                {
                    Start1 = start1 - precontext.Length,
                    Start2 = start2 - precontext.Length
                };

                if (precontext.Length != 0)
                {
                    patch = patch with
                    {
                        Length1 = precontext.Length,
                        Length2 = precontext.Length,
                        Diffs = patch.Diffs.Add(new Diff<char>(Operation.EQUAL, precontext))
                    };
                }

                while (bigpatch.Diffs.Count != 0 && patch.Length1 < patch_size - options.Margin)
                {
                    Operation diff_type = bigpatch.Diffs[0].Operation;
                    var diff_text = bigpatch.Diffs[0].Text;
                    if (diff_type == Operation.INSERT)
                    {
                        // Insertions are harmless.
                        patch = patch with
                        {
                            Length2 = patch.Length2 + diff_text.Length,
                            Diffs = patch.Diffs.Add(bigpatch.Diffs.First())
                        };

                        start2 += diff_text.Length;
                        bigpatch = bigpatch with { Diffs = bigpatch.Diffs.RemoveAt(0) };
                        empty = false;
                    }
                    else if (diff_type == Operation.DELETE && patch.Diffs.Count == 1
                        && patch.Diffs.First().Operation == Operation.EQUAL
                        && diff_text.Length > 2 * patch_size)
                    {
                        // This is a large deletion.  Let it pass in one chunk.
                        patch = patch with
                        {
                            Length1 = patch.Length1 + diff_text.Length,
                            Diffs = patch.Diffs.Add(new Diff<char>(diff_type, diff_text))
                        };
                        start1 += diff_text.Length;
                        empty = false;
                        bigpatch = bigpatch with { Diffs = bigpatch.Diffs.RemoveAt(0) };
                    }
                    else
                    {
                        // Deletion or equality.  Only take as much as we can stomach.
                        diff_text = diff_text.Slice(0, Math.Min(diff_text.Length, patch_size - patch.Length1 - options.Margin));
                        patch = patch with { Length1 = patch.Length1 + diff_text.Length };
                        start1 += diff_text.Length;
                        if (diff_type == Operation.EQUAL)
                        {
                            patch = patch with { Length2 = patch.Length2 + diff_text.Length };
                            start2 += diff_text.Length;
                        }
                        else
                        {
                            empty = false;
                        }

                        patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(diff_type, diff_text)) };
                        if (diff_text == bigpatch.Diffs[0].Text)
                        {
                            bigpatch = bigpatch with { Diffs = bigpatch.Diffs.RemoveAt(0) };
                        }
                        else
                        {
                            bigpatch = bigpatch with { Diffs = bigpatch.Diffs.SetItem(0, bigpatch.Diffs[0].WithText(bigpatch.Diffs[0].Text.Slice(diff_text.Length))) };
                        }
                    }
                }

                // Compute the head context for the next patch.
                precontext = patch.Diffs.ToTarget();
                precontext = precontext.Slice(Math.Max(0, precontext.Length - options.Margin));

                var postcontext = Rope<char>.Empty;
                // Append the end context for this patch.
                if (bigpatch.Diffs.ToSource().Length > options.Margin)
                {
                    postcontext = bigpatch.Diffs.ToSource().Slice(0, options.Margin);
                }
                else
                {
                    postcontext = bigpatch.Diffs.ToSource();
                }

                if (postcontext.Length != 0)
                {
                    patch = patch with
                    {
                        Length1 = patch.Length1 + postcontext.Length,
                        Length2 = patch.Length2 + postcontext.Length
                    };

                    if (patch.Diffs.Count != 0 && patch.Diffs[patch.Diffs.Count - 1].Operation == Operation.EQUAL)
                    {
                        patch = patch with
                        {
                            Diffs = patch.Diffs.SetItem(patch.Diffs.Count - 1, patch.Diffs[patch.Diffs.Count - 1].Append(postcontext))
                        };
                    }
                    else
                    {
                        patch = patch with
                        {
                            Diffs = patch.Diffs.Add(new Diff<char>(Operation.EQUAL, postcontext))
                        };
                    }
                }

                if (!empty)
                {
                    (_, results) = results.Splice(++x, 0, patch);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Take a list of patches and return a textual representation.
    /// </summary>
    /// <param name="patches">List of Patch objects.</param>
    /// <returns>Text representation of patches.</returns>
    public string ToPatchText(IEnumerable<Patch<char>> patches)
    {
        StringBuilder text = new StringBuilder();
        foreach (var aPatch in patches)
        {
            text.Append(aPatch);
        }

        return text.ToString();
    }

    /// <summary>
    /// Parse a textual representation of patches and return a List of Patch
    /// objects.
    /// </summary>
    /// <param name="textline">Text representation of patches.</param>
    /// <returns>List of Patch objects.</returns>
    /// <exception cref="ArgumentException">Thrown if invalid input.</exception>
    public List<Patch<char>> ParsePatchText(string textline)
    {
        List<Patch<char>> patches = new List<Patch<char>>();
        if (textline.Length == 0)
        {
            return patches;
        }

        string[] text = textline.Split('\n');
        int textPointer = 0;
        Patch<char> patch;
        Regex patchHeader = new Regex("^@@ -(\\d+),?(\\d*) \\+(\\d+),?(\\d*) @@$");
        Match m;
        char sign;
        string line;
        while (textPointer < text.Length)
        {
            m = patchHeader.Match(text[textPointer]);
            if (!m.Success)
            {
                throw new ArgumentException("Invalid patch string: " + text[textPointer]);
            }

            patch = new Patch<char>()
            {
                Start1 = Convert.ToInt32(m.Groups[1].Value)
            };

            if (m.Groups[2].Length == 0)
            {
                patch = patch with { Start1 = patch.Start1 - 1, Length1 = 1 };
            }
            else if (m.Groups[2].Value == "0")
            {
                patch = patch with { Length1 = 0 };
            }
            else
            {
                patch = patch with { Start1 = patch.Start1 - 1, Length1 = Convert.ToInt32(m.Groups[2].Value) };
            }

            patch = patch with { Start2 = Convert.ToInt32(m.Groups[3].Value) };
            if (m.Groups[4].Length == 0)
            {
                patch = patch with { Start2 = patch.Start2 - 1, Length2 = 1 };
            }
            else if (m.Groups[4].Value == "0")
            {
                patch = patch with { Length2 = 0 };
            }
            else
            {
                patch = patch with { Start2 = patch.Start2 - 1, Length2 = Convert.ToInt32(m.Groups[4].Value) };
            }

            textPointer++;
            while (textPointer < text.Length)
            {
                try
                {
                    sign = text[textPointer][0];
                }
                catch (IndexOutOfRangeException)
                {
                    // Blank line?  Whatever.
                    textPointer++;
                    continue;
                }
                line = text[textPointer].Substring(1);
                line = line.Replace("+", "%2b");
                line = HttpUtility.UrlDecode(line);
                if (sign == '-')
                {
                    // Deletion.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(Operation.DELETE, line)) };
                }
                else if (sign == '+')
                {
                    // Insertion.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(Operation.INSERT, line)) };
                }
                else if (sign == ' ')
                {
                    // Minor equality.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(Operation.EQUAL, line)) };
                }
                else if (sign == '@')
                {
                    // Start of next patch.
                    break;
                }
                else
                {
                    // WTF?
                    throw new ArgumentException("Invalid patch mode '" + sign + "' in: " + line);
                }

                textPointer++;
            }

            patches.Add(patch);

        }

        return patches;
    }
}