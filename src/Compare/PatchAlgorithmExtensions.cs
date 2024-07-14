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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

public static partial class PatchAlgorithmExtensions
{
    private static readonly Regex PatchHeaderPattern = CreatePatchHeaderPattern();

    [GeneratedRegex("^@@ -(\\d+),?(\\d*) \\+(\\d+),?(\\d*) @@$")]
    private static partial Regex CreatePatchHeaderPattern();

    /// <summary>
    /// Parse a textual representation of patches and return a List of Patch
    /// objects.
    /// </summary>
    /// <param name="patchText">Text representation of patches.</param>
    /// <returns>List of Patch objects.</returns>
    /// <exception cref="ArgumentException">Thrown if invalid input.</exception>
    //public static Rope<Patch<T>> ParsePatchText<T>(this Rope<char> patchText, Func<Rope<char>, T> parseItem, char separator = '~') where T : IEquatable<T>
    //{
    //    var patches = Rope<Patch<T>>.Empty;
    //    if (patchText.Length == 0)
    //    {
    //        return patches;
    //    }

    //    var text = patchText.Split('\n').ToRope();
    //    long textPointer = 0;
    //    Patch<T> patch;
    //    Match m;
    //    char sign;
    //    Rope<char> line = Rope<char>.Empty;
    //    while (textPointer < text.Length)
    //    {
    //        var pointerText = text[textPointer].ToString();
    //        m = PatchHeaderPattern.Match(pointerText);
    //        if (!m.Success)
    //        {
    //            throw new ArgumentException("Invalid patch string: " + pointerText);
    //        }

    //        patch = new Patch<T>()
    //        {
    //            Start1 = Convert.ToInt32(m.Groups[1].Value)
    //        };

    //        if (m.Groups[2].Length == 0)
    //        {
    //            patch = patch with { Start1 = patch.Start1 - 1, Length1 = 1 };
    //        }
    //        else if (m.Groups[2].Value == "0")
    //        {
    //            patch = patch with { Length1 = 0 };
    //        }
    //        else
    //        {
    //            patch = patch with { Start1 = patch.Start1 - 1, Length1 = Convert.ToInt32(m.Groups[2].Value) };
    //        }

    //        patch = patch with { Start2 = Convert.ToInt32(m.Groups[3].Value) };
    //        if (m.Groups[4].Length == 0)
    //        {
    //            patch = patch with { Start2 = patch.Start2 - 1, Length2 = 1 };
    //        }
    //        else if (m.Groups[4].Value == "0")
    //        {
    //            patch = patch with { Length2 = 0 };
    //        }
    //        else
    //        {
    //            patch = patch with { Start2 = patch.Start2 - 1, Length2 = Convert.ToInt32(m.Groups[4].Value) };
    //        }

    //        textPointer++;
    //        while (textPointer < text.Length)
    //        {
    //            try
    //            {
    //                sign = text[textPointer][0];
    //            }
    //            catch (IndexOutOfRangeException)
    //            {
    //                // Blank line?  Whatever.
    //                textPointer++;
    //                continue;
    //            }

    //            line = text[textPointer].Slice(1);
    //            line = line.Replace("+", "%2b");
    //            line = HttpUtility.UrlDecode(line.ToString());
    //            if (sign == '-')
    //            {
    //                // Deletion.
    //                var items = line.Split(separator).Select(i => parseItem(i)).ToRope();
    //                patch = patch with { Diffs = patch.Diffs.Add(new Diff<T>(Operation.Delete, items)) };
    //            }
    //            else if (sign == '+')
    //            {
    //                // Insertion.
    //                var items = line.Split(separator).Select(i => parseItem(i)).ToRope();
    //                patch = patch with { Diffs = patch.Diffs.Add(new Diff<T>(Operation.Insert, items)) };
    //            }
    //            else if (sign == ' ')
    //            {
    //                // Minor equality.
    //                var items = line.Split(separator).Select(i => parseItem(i)).ToRope();
    //                patch = patch with { Diffs = patch.Diffs.Add(new Diff<T>(Operation.Equal, items)) };
    //            }
    //            else if (sign == '@')
    //            {
    //                // Start of next patch.
    //                break;
    //            }
    //            else
    //            {
    //                // WTF?
    //                throw new ArgumentException("Invalid patch mode '" + sign + "' in: " + line.ToString());
    //            }

    //            textPointer++;
    //        }

    //        patches = patches.Add(patch);

    //    }

    //    return patches;
    //}

    /// <summary>
    /// Parse a textual representation of patches and return a List of Patch
    /// objects.
    /// </summary>
    /// <param name="patchText">Text representation of patches.</param>
    /// <returns>List of Patch objects.</returns>
    /// <exception cref="ArgumentException">Thrown if invalid input.</exception>
    public static Rope<Patch<char>> ParsePatchText(this Rope<char> patchText)
    {
        var patches = Rope<Patch<char>>.Empty;
        if (patchText.Length == 0)
        {
            return patches;
        }

        var text = patchText.Split('\n').ToRope();
        long textPointer = 0;
        Patch<char> patch;
        Match m;
        char sign;
        Rope<char> line = Rope<char>.Empty;
        while (textPointer < text.Length)
        {
            var pointerText = text[textPointer].ToString();
            m = PatchHeaderPattern.Match(pointerText);
            if (!m.Success)
            {
                throw new ArgumentException("Invalid patch string: " + pointerText);
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

                line = text[textPointer].Slice(1);
                line = line.Replace("+", "%2b");
                line = HttpUtility.UrlDecode(line.ToString());
                if (sign == '-')
                {
                    // Deletion.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(Operation.Delete, line)) };
                }
                else if (sign == '+')
                {
                    // Insertion.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(Operation.Insert, line)) };
                }
                else if (sign == ' ')
                {
                    // Minor equality.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(Operation.Equal, line)) };
                }
                else if (sign == '@')
                {
                    // Start of next patch.
                    break;
                }
                else
                {
                    // WTF?
                    throw new ArgumentException("Invalid patch mode '" + sign + "' in: " + line.ToString());
                }

                textPointer++;
            }

            patches = patches.Add(patch);

        }

        return patches;
    }

    /// <summary>
    /// Take a list of patches and return a textual representation.
    /// </summary>
    /// <param name="patches">List of Patch objects.</param>
    /// <returns>Text representation of patches.</returns>
    //public static string ToPatchText<T>(this IEnumerable<Patch<T>> patches, Func<T, Rope<char>> itemToString) where T : IEquatable<T>
    //{
    //    StringBuilder text = new StringBuilder();
    //    foreach (var aPatch in patches)
    //    {
    //        text.Append(aPatch.ToCharRope(itemToString).ToString());
    //    }

    //    return text.ToString();
    //}

    /// <summary>
    /// Take a list of patches and return a textual representation.
    /// </summary>
    /// <param name="patches">List of Patch objects.</param>
    /// <returns>Text representation of patches.</returns>
    public static string ToPatchText(this IEnumerable<Patch<char>> patches)
    {
        StringBuilder text = new StringBuilder();
        foreach (var aPatch in patches)
        {
            text.Append(aPatch);
        }

        return text.ToString();
    }

    [Pure]
    public static Rope<Patch<char>> CreatePatches(this string text1, string text2, PatchOptions? patchOptions = null, DiffOptions<char>? diffOptions = null) => CreatePatches(text1.ToRope(), text2.ToRope(), patchOptions, diffOptions);

    /// <summary>
    /// Compute a list of patches to turn text1 into text2.
    /// A set of diffs will be computed.
    /// </summary>
    /// <param name="text1">Old text.</param>
    /// <param name="text2">New text.</param>
    /// <returns>List of Patch objects.</returns>
    [Pure]
    public static Rope<Patch<T>> CreatePatches<T>(this Rope<T> text1, Rope<T> text2, PatchOptions? patchOptions, DiffOptions<T>? diffOptions = null) where T : IEquatable<T>
    {
        // No diffs provided, compute our own.
        diffOptions = diffOptions ?? DiffOptions<T>.Default;
        using var deadline = diffOptions.StartTimer();
        var diffs = text1.Diff(text2, diffOptions, deadline.Cancellation);
        if (diffs.Count > 2)
        {
            diffs = diffs.DiffCleanupSemantic(deadline.Cancellation);
            diffs = diffs.DiffCleanupEfficiency(diffOptions);
        }

        return text1.ToPatches(diffs, patchOptions);
    }

    /// <summary>
    /// Compute a list of patches to turn text1 into text2.
    /// text1 will be derived from the provided diffs.
    /// </summary>
    /// <param name="diffs">List of Diff objects for text1 to text2.</param>
    /// <returns>List of Patch objects.</returns>
    [Pure]
    public static Rope<Patch<T>> ToPatches<T>(this Rope<Diff<T>> diffs) where T : IEquatable<T>
    {
        // No origin string provided, compute our own.
        var text1 = diffs.ToSource();
        return text1.ToPatches(diffs, PatchOptions.Default);
    }

    /// <summary>
    /// Compute a list of patches to turn text1 into text2.
    /// text2 is not provided, diffs are the delta between text1 and text2.
    /// </summary>
    /// <param name="text1">Old text.</param>
    /// <param name="diffs">Sequence of Diff objects for text1 to text2.</param>
    /// <param name="options">Options controlling how the patches are created.</param>
    /// <returns>List of Patch objects.</returns>
    [Pure]
    public static Rope<Patch<T>> ToPatches<T>(this Rope<T> text1, Rope<Diff<T>> diffs, PatchOptions? options) where T : IEquatable<T>
    {
        options ??= PatchOptions.Default;
        // Check for null inputs not needed since null can't be passed in C#.
        if (diffs.Count == 0)
        {
            return Rope<Patch<T>>.Empty;
        }

        var result = Rope<Patch<T>>.Empty;
        var patch = new Patch<T>();
        long char_count1 = 0;  // Number of characters into the text1 string.
        long char_count2 = 0;  // Number of characters into the text2 string.
                               // Start with text1 (prepatch_text) and apply the diffs until we arrive at
                               // text2 (postpatch_text). We recreate the patches one by one to determine
                               // context info.
        var prepatch_text = text1;
        var postpatch_text = text1;
        foreach (var aDiff in diffs)
        {
            if (patch.Diffs.Count == 0 && aDiff.Operation != Operation.Equal)
            {
                // A new patch starts here.
                patch = patch with
                {
                    Start1 = char_count1,
                    Start2 = char_count2
                };
            }

            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    patch = patch with { Diffs = patch.Diffs.Add(aDiff), Length2 = patch.Length2 + aDiff.Items.Length };
                    postpatch_text = postpatch_text.InsertRange(char_count2, aDiff.Items);
                    break;
                case Operation.Delete:
                    patch = patch with { Length1 = patch.Length1 + aDiff.Items.Length, Diffs = patch.Diffs.Add(aDiff) };
                    postpatch_text = postpatch_text.RemoveRange(char_count2, aDiff.Items.Length);
                    break;
                case Operation.Equal:
                    if (aDiff.Items.Length <= 2 * options.Margin && patch.Diffs.Count != 0 && aDiff != diffs.Last())
                    {
                        // Small equality inside a patch.
                        patch = patch with
                        {
                            Diffs = patch.Diffs.Add(aDiff),
                            Length1 = patch.Length1 + aDiff.Items.Length,
                            Length2 = patch.Length2 + aDiff.Items.Length
                        };
                    }

                    if (aDiff.Items.Length >= 2 * options.Margin)
                    {
                        // Time for a new patch.
                        if (patch.Diffs.Count != 0)
                        {
                            patch = patch.PatchAddContext(prepatch_text, options);
                            result += patch;

                            patch = new Patch<T>();
                            // Unlike Unidiff, our patch lists have a rolling context.
                            // https://github.com/google/diff-match-patch/wiki/Unidiff
                            // Update prepatch text & pos to reflect the application of the
                            // just completed patch.
                            prepatch_text = postpatch_text;
                            char_count1 = char_count2;
                        }
                    }
                    break;
            }

            // Update the current character count.
            if (aDiff.Operation != Operation.Insert)
            {
                char_count1 += aDiff.Items.Length;
            }
            if (aDiff.Operation != Operation.Delete)
            {
                char_count2 += aDiff.Items.Length;
            }
        }

        // Pick up the leftover patch if not empty.
        if (patch.Diffs.Count != 0)
        {
            patch = patch.PatchAddContext(prepatch_text, options);
            result += patch;
        }

        return result;
    }

    /// <summary>
    /// Merge a set of patches onto the text. Returns the patched text, as well
    /// as an array of true/false values indicating which patches were applied.
    /// </summary>
    /// <param name="patches">Array of Patch objects</param>
    /// <param name="text">Old text.</param>
    /// <returns>Value tuple containing the new text and an array of
    /// bool values for whether each patch was applied.</returns>
    [Pure]
    public static (string Text, bool[] Applied) ApplyPatches(this IEnumerable<Patch<char>> patches, string text, PatchOptions options)
    {
        var (result, applied) = ApplyPatches(patches.ToRope(), text.ToRope(), options);
        return (result.ToString(), applied);
    }

    /// <summary>
    /// Merge a set of patches onto the text. Returns the patched text, as well
    /// as an array of true/false values indicating which patches were applied.
    /// </summary>
    /// <param name="patches">Array of Patch objects</param>
    /// <param name="text">Old text.</param>
    /// <returns>Value tuple containing the new rope and an array of
    /// bool values for whether each patch was applied.</returns>
    [Pure]
    public static (Rope<char> Text, bool[] Applied) ApplyPatches(this Rope<Patch<char>> patches, Rope<char> text, PatchOptions options, DiffOptions<char>? diffOptions = null)
    {
        if (patches.Count == 0)
        {
            return (text, Array.Empty<bool>());
        }

        (var nullPadding, patches) = patches.PatchAddPadding(options);
        text = nullPadding + text + nullPadding;
        patches = patches.PatchSplitMaxLength(options);

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
                start_loc = text.MatchPattern(text1.Slice(0, options.MaxLength), expected_loc, options);
                if (start_loc != -1)
                {
                    end_loc = text.MatchPattern(
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
                start_loc = text.MatchPattern(text1, expected_loc, options);
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
                    var diffs = text1.Diff(text2, (diffOptions ?? DiffOptions<char>.Default).WithChunking(false));
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
                            if (aDiff.Operation != Operation.Equal)
                            {
                                var index2 = diffs.TranslateToTargetIndex(index1);
                                if (aDiff.Operation == Operation.Insert)
                                {
                                    // Insertion
                                    text = text.InsertRange(start_loc + index2, aDiff.Items);
                                }
                                else if (aDiff.Operation == Operation.Delete)
                                {
                                    // Deletion
                                    text = text.RemoveRange(start_loc + index2, diffs.TranslateToTargetIndex(index1 + aDiff.Items.Length) - index2);
                                }
                            }

                            if (aDiff.Operation != Operation.Delete)
                            {
                                index1 += aDiff.Items.Length;
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

    [Pure]
    public static Rope<Patch<char>> PatchSplitMaxLength(this IEnumerable<Patch<char>> patches, PatchOptions options)
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
                        Diffs = patch.Diffs.Add(new Diff<char>(Operation.Equal, precontext))
                    };
                }

                while (bigpatch.Diffs.Count != 0 && patch.Length1 < patch_size - options.Margin)
                {
                    Operation diff_type = bigpatch.Diffs[0].Operation;
                    var diff_text = bigpatch.Diffs[0].Items;
                    if (diff_type == Operation.Insert)
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
                    else if (diff_type == Operation.Delete && patch.Diffs.Count == 1
                        && patch.Diffs.First().Operation == Operation.Equal
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
                        if (diff_type == Operation.Equal)
                        {
                            patch = patch with { Length2 = patch.Length2 + diff_text.Length };
                            start2 += diff_text.Length;
                        }
                        else
                        {
                            empty = false;
                        }

                        patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(diff_type, diff_text)) };
                        if (diff_text == bigpatch.Diffs[0].Items)
                        {
                            bigpatch = bigpatch with { Diffs = bigpatch.Diffs.RemoveAt(0) };
                        }
                        else
                        {
                            bigpatch = bigpatch with { Diffs = bigpatch.Diffs.SetItem(0, bigpatch.Diffs[0].WithItems(bigpatch.Diffs[0].Items.Slice(diff_text.Length))) };
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

                    if (patch.Diffs.Count != 0 && patch.Diffs[patch.Diffs.Count - 1].Operation == Operation.Equal)
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
                            Diffs = patch.Diffs.Add(new Diff<char>(Operation.Equal, postcontext))
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
    /// Add some padding on text start and end so that edges can match something.
    /// Intended to be called only from within patch_apply.
    /// <param name="patches">Array of Patch objects.</param>
    /// <param name="options"></param>
    /// <returns>The padding string added to each side.</returns>
    [Pure]
    internal static (Rope<char> NullPadding, Rope<Patch<char>> BumpedPatches) PatchAddPadding(this Rope<Patch<char>> patches, PatchOptions options)
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
        if (diffs.Count == 0 || diffs[0].Operation != Operation.Equal)
        {
            // Add nullPadding equality.
            patch = patch with
            {
                Start1 = patch.Start1 - paddingLength,  // Should be 0.
                Start2 = patch.Start2 - paddingLength,  // Should be 0.
                Length1 = patch.Length1 + paddingLength,
                Length2 = patch.Length2 + paddingLength,
                Diffs = diffs.Insert(0, new Diff<char>(Operation.Equal, nullPadding))
            };
        }
        else if (paddingLength > diffs[0].Items.Length)
        {
            // Grow first equality.
            var firstDiff = diffs[0];
            var extraLength = paddingLength - firstDiff.Items.Length;

            patch = patch with
            {
                Start1 = patch.Start1 - extraLength,
                Start2 = patch.Start2 - extraLength,
                Length1 = patch.Length1 + extraLength,
                Length2 = patch.Length2 + extraLength,
                Diffs = diffs.SetItem(0, firstDiff.Prepend(nullPadding.Slice(firstDiff.Items.Length)))
            };
        }

        bumpedPatches = bumpedPatches.SetItem(0, patch);

        // Add some padding on end of last diff.
        patch = bumpedPatches[^1];
        diffs = patch.Diffs;
        if (diffs.Count == 0 || diffs.Last().Operation != Operation.Equal)
        {
            // Add nullPadding equality.
            patch = patch with
            {
                Length1 = patch.Length1 + paddingLength,
                Length2 = patch.Length2 + paddingLength,
                Diffs = diffs.Add(new Diff<char>(Operation.Equal, nullPadding)),
            };
        }
        else if (paddingLength > diffs[^1].Items.Length)
        {
            // Grow last equality.
            var lastDiff = diffs[^1];
            var extraLength = paddingLength - lastDiff.Items.Length;
            patch = patch with
            {
                Length1 = patch.Length1 + extraLength,
                Length2 = patch.Length2 + extraLength,
                Diffs = diffs.SetItem(diffs.Count - 1, lastDiff.Append(nullPadding.Slice(0, extraLength)))
            };
        }

        bumpedPatches = bumpedPatches.SetItem(bumpedPatches.Length - 1, patch);
        return (nullPadding, bumpedPatches);
    }

    [Pure]
    internal static Patch<T> PatchAddContext<T>(this Patch<T> patch, Rope<T> text, PatchOptions options) where T : IEquatable<T>
    {
        if (text.Length == 0)
        {
            return patch;
        }

        var pattern = text.Slice(patch.Start2, patch.Length1);
        int padding = 0;

        // Look for the first and last matches of pattern in text.  If two
        // different matches are found, increase the pattern length.
        while (text.IndexOf(pattern) != text.LastIndexOf(pattern) && pattern.Length < options.MaxLength - options.Margin - options.Margin)
        {
            padding += options.Margin;
            pattern = text.JavaSubstring(Math.Max(0, patch.Start2 - padding), Math.Min(text.Length, patch.Start2 + patch.Length1 + padding));
        }

        // Add one chunk for good luck.
        padding += options.Margin;

        // Add the prefix.
        var prefix = text.JavaSubstring(Math.Max(0, patch.Start2 - padding), patch.Start2);
        if (prefix.Length != 0)
        {
            patch = patch with { Diffs = patch.Diffs.Insert(0, new Diff<T>(Operation.Equal, prefix)) };
        }

        // Add the suffix.
        var suffix = text.JavaSubstring(patch.Start2 + patch.Length1, Math.Min(text.Length, patch.Start2 + patch.Length1 + padding));
        if (suffix.Length != 0)
        {
            patch = patch with { Diffs = patch.Diffs.Add(new Diff<T>(Operation.Equal, suffix)) };
        }

        // Roll back the start points.
        // Extend the lengths.
        return patch with
        {
            Start1 = patch.Start1 - prefix.Length,
            Start2 = patch.Start2 - prefix.Length,
            Length1 = patch.Length1 + prefix.Length + suffix.Length,
            Length2 = patch.Length2 + prefix.Length + suffix.Length
        };
    }
}
