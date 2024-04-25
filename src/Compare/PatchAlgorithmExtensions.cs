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

public static class PatchAlgorithmExtensions
{
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
            if (patch.Diffs.Count == 0 && aDiff.Operation != Operation.EQUAL)
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
                case Operation.INSERT:
                    patch = patch with { Diffs = patch.Diffs.Add(aDiff), Length2 = patch.Length2 + aDiff.Text.Length };
                    postpatch_text = postpatch_text.InsertRange(char_count2, aDiff.Text);
                    break;
                case Operation.DELETE:
                    patch = patch with { Length1 = patch.Length1 + aDiff.Text.Length, Diffs = patch.Diffs.Add(aDiff) };
                    postpatch_text = postpatch_text.RemoveRange(char_count2, aDiff.Text.Length);
                    break;
                case Operation.EQUAL:
                    if (aDiff.Text.Length <= 2 * options.Margin && patch.Diffs.Count != 0 && aDiff != diffs.Last())
                    {
                        // Small equality inside a patch.
                        patch = patch with
                        {
                            Diffs = patch.Diffs.Add(aDiff),
                            Length1 = patch.Length1 + aDiff.Text.Length,
                            Length2 = patch.Length2 + aDiff.Text.Length
                        };
                    }

                    if (aDiff.Text.Length >= 2 * options.Margin)
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
            if (aDiff.Operation != Operation.INSERT)
            {
                char_count1 += aDiff.Text.Length;
            }
            if (aDiff.Operation != Operation.DELETE)
            {
                char_count2 += aDiff.Text.Length;
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
            patch = patch with { Diffs = patch.Diffs.Insert(0, new Diff<T>(Operation.EQUAL, prefix)) };
        }

        // Add the suffix.
        var suffix = text.JavaSubstring(patch.Start2 + patch.Length1, Math.Min(text.Length, patch.Start2 + patch.Length1 + padding));
        if (suffix.Length != 0)
        {
            patch = patch with { Diffs = patch.Diffs.Add(new Diff<T>(Operation.EQUAL, suffix)) };
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
