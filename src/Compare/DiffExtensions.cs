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
using System.Collections.Generic;
using System.Diagnostics.Contracts;

/// <summary>
/// Series of extensions on a sequence of <see cref="Diff{T}"/> operations.
/// </summary>
public static class DiffExtensions
{
    /// <summary>
    /// Convert a Diff list into a pretty HTML report.
    /// </summary>
    /// <param name="diffs">List of Diff objects.</param>
    /// <returns>HTML string representation of the diff.</returns>
    [Pure]
    public static Rope<char> ToHtmlReport<T>(this IEnumerable<Diff<T>> diffs) where T : IEquatable<T>
    {
        var html = Rope<char>.Empty;
        foreach (Diff<T> aDiff in diffs)
        {
            var text = aDiff.Items.SelectMany(c => c.ToString() ?? string.Empty).ToRope()
                .Replace("&".ToRope(), "&amp;".ToRope())
                .Replace("<".ToRope(), "&lt;".ToRope())
                .Replace(">".ToRope(), "&gt;".ToRope())
                .Replace("\n".ToRope(), "&para;<br>".ToRope());
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    html = html.AddRange("<ins style=\"background:#e6ffe6;\">".ToRope())
                        .AddRange(text)
                        .AddRange("</ins>".ToRope());
                    break;
                case Operation.Delete:
                    html = html.AddRange("<del style=\"background:#ffe6e6;\">".ToRope())
                        .AddRange(text)
                        .AddRange("</del>".ToRope());
                    break;
                case Operation.Equal:
                    html = html.AddRange("<span>".ToRope())
                        .AddRange(text)
                        .AddRange("</span>".ToRope());
                    break;
            }
        }

        return html;
    }

    /// <summary>
    /// Compute and return the source text from a list of diff operations.
    /// Includes all equalities and deletions.
    /// </summary>
    /// <param name="diffs">List of Diff objects</param>
    /// <returns>Source text.</returns>
    [Pure]
    public static Rope<T> ToSource<T>(this IEnumerable<Diff<T>> diffs) where T : IEquatable<T> =>
            (from aDiff in diffs
             where aDiff.Operation != Operation.Insert
             select aDiff.Items).Combine();

    /// <summary>
    /// Compute and return the target text from a list of diff operations.
    /// Includes all equalities and insertions.
    /// </summary>
    /// <param name="diffs">List of Diff objects</param>
    /// <returns>Target text.</returns>
    [Pure]
    public static Rope<T> ToTarget<T>(this IEnumerable<Diff<T>> diffs) where T : IEquatable<T> =>
        (from aDiff in diffs
         where aDiff.Operation != Operation.Delete
         select aDiff.Items).Combine();

    /// <summary>
    /// Compute and return the source and target texts from a list of diff operations.
    /// Includes all equalities and deletions in the source, and all equalities and insertions
    /// in the target.
    /// </summary>
    /// <param name="diffs">List of Diff objects</param>
    /// <returns>Both the Source and Target text.</returns>
    public static (Rope<T> Source, Rope<T> Target) ToSourceAndTarget<T>(this IEnumerable<Diff<T>> diffs) where T : IEquatable<T> =>
        (diffs.ToSource(), diffs.ToTarget());

    /// <summary>
    /// Given an index in the source, compute and return the equivalent index in target in terms of the diffs.
    /// e.g. "The cat" vs "The big cat", 1->1, 5->8
    /// </summary>
    /// <param name="diffs">List of Diff objects.</param>
    /// <param name="sourceIndex">Location within sourceText.</param>
    /// <returns>Location within text2.</returns>
    [Pure]
    public static long TranslateToTargetIndex<T>(this IEnumerable<Diff<T>> diffs, long sourceIndex) where T : IEquatable<T>
    {
        long chars1 = 0;
        long chars2 = 0;
        long last_chars1 = 0;
        long last_chars2 = 0;
        Diff<T> lastDiff = default;
        foreach (var aDiff in diffs)
        {
            if (aDiff.Operation != Operation.Insert)
            {
                // Equality or deletion.
                chars1 += aDiff.Items.Length;
            }
            if (aDiff.Operation != Operation.Delete)
            {
                // Equality or insertion.
                chars2 += aDiff.Items.Length;
            }
            if (chars1 > sourceIndex)
            {
                // Overshot the location.
                lastDiff = aDiff;
                break;
            }
            last_chars1 = chars1;
            last_chars2 = chars2;
        }

        if (lastDiff != default && lastDiff.Operation == Operation.Delete)
        {
            // The location was deleted.
            return last_chars2;
        }

        // Add the remaining character length.
        return last_chars2 + (sourceIndex - last_chars1);
    }

    /// <summary>
    /// Crush the diff into an encoded string which describes the operations
    /// required to transform source into target.
    /// E.g. =3\t-2\t+ing  -> Keep 3 chars, delete 2 chars, insert 'ing'.
    /// Operations are tab-separated. Inserted text is escaped using %xx Url-like encoding notation.
    /// </summary>
    /// <param name="diffs">Array of Diff objects.</param>
    /// <returns>Delta text.</returns>
    [Pure]
    public static Rope<char> ToDelta(this IEnumerable<Diff<char>> diffs)
    {
        var text = Rope<char>.Empty;
        foreach (var aDiff in diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    text = text.Append("+").AddRange(aDiff.Items.DiffEncode()).Append("\t");
                    break;
                case Operation.Delete:
                    text = text.Append("-").Append(aDiff.Items.Length.ToString()).Append("\t");
                    break;
                case Operation.Equal:
                    text = text.Append("=").Append(aDiff.Items.Length.ToString()).Append("\t");
                    break;
            }
        }

        if (text.Length != 0)
        {
            // Strip off trailing tab character.
            text = text.Slice(0, text.Length - 1);
        }

        return text;
    }

    /// <summary>
    /// Crush the diff into an encoded string which describes the operations
    /// required to transform source into target.
    /// E.g. =3\t-2\t+ing  -> Keep 3 chars, delete 2 chars, insert 'ing'.
    /// Operations are tab-separated. Inserted text is escaped using %xx Url-like encoding notation.
    /// </summary>
    /// <param name="diffs">Array of Diff objects.</param>
    /// <returns>Delta text.</returns>
    [Pure]
    public static Rope<char> ToDelta<T>(this IEnumerable<Diff<T>> diffs, Func<T?, Rope<char>> itemToString) where T : IEquatable<T>
    {
        var text = Rope<char>.Empty;
        foreach (var aDiff in diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    text = text.Append("+").AddRange(aDiff.Items.DiffEncode(itemToString)).Append("\t");
                    break;
                case Operation.Delete:
                    text = text.Append("-").Append(aDiff.Items.Length.ToString()).Append("\t");
                    break;
                case Operation.Equal:
                    text = text.Append("=").Append(aDiff.Items.Length.ToString()).Append("\t");
                    break;
            }
        }

        if (text.Length != 0)
        {
            // Strip off trailing tab character.
            text = text.Slice(0, text.Length - 1);
        }

        return text;
    }

    /// <summary>
    /// Compute the Levenshtein distance; the number of inserted, deleted or
    /// substituted characters.
    /// </summary>
    /// <param name="diffs">List of Diff objects.</param>
    /// <returns>Number of changes.</returns>
    [Pure]
    public static long CalculateEditDistance<T>(this IEnumerable<Diff<T>> diffs) where T : IEquatable<T>
    {
        long levenshtein = 0;
        long insertions = 0;
        long deletions = 0;
        foreach (var aDiff in diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    insertions += aDiff.Items.Length;
                    break;
                case Operation.Delete:
                    deletions += aDiff.Items.Length;
                    break;
                case Operation.Equal:
                    // A deletion and an insertion is one substitution.
                    levenshtein += Math.Max(insertions, deletions);
                    insertions = 0;
                    deletions = 0;
                    break;
            }
        }

        levenshtein += Math.Max(insertions, deletions);
        return levenshtein;
    }
}
