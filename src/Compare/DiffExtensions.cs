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
            var text = aDiff.Text.SelectMany(c => c.ToString() ?? string.Empty).ToRope()
                .Replace("&".ToRope(), "&amp;".ToRope())
                .Replace("<".ToRope(), "&lt;".ToRope())
                .Replace(">".ToRope(), "&gt;".ToRope())
                .Replace("\n".ToRope(), "&para;<br>".ToRope());
            switch (aDiff.Operation)
            {
                case Operation.INSERT:
                    html = html.AddRange("<ins style=\"background:#e6ffe6;\">".ToRope())
                        .AddRange(text)
                        .AddRange("</ins>".ToRope());
                    break;
                case Operation.DELETE:
                    html = html.AddRange("<del style=\"background:#ffe6e6;\">".ToRope())
                        .AddRange(text)
                        .AddRange("</del>".ToRope());
                    break;
                case Operation.EQUAL:
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
             where aDiff.Operation != Operation.INSERT
             select aDiff.Text).Combine();      

    /// <summary>
    /// Compute and return the target text from a list of diff operations.
    /// Includes all equalities and insertions.
    /// </summary>
    /// <param name="diffs">List of Diff objects</param>
    /// <returns>Target text.</returns>
    [Pure]
    public static Rope<T> ToTarget<T>(this IEnumerable<Diff<T>> diffs) where T : IEquatable<T> =>
        (from aDiff in diffs
         where aDiff.Operation != Operation.DELETE
         select aDiff.Text).Combine();

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
            if (aDiff.Operation != Operation.INSERT)
            {
                // Equality or deletion.
                chars1 += aDiff.Text.Length;
            }
            if (aDiff.Operation != Operation.DELETE)
            {
                // Equality or insertion.
                chars2 += aDiff.Text.Length;
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

        if (lastDiff != default && lastDiff.Operation == Operation.DELETE)
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
    public static Rope<char> ToDelta<T>(this IEnumerable<Diff<T>> diffs) where T : IEquatable<T>
    {
        var text = Rope<char>.Empty;
        foreach (var aDiff in diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.INSERT:
                    text = text.Append("+").AddRange(aDiff.Text.DiffEncode()).Append("\t");
                    break;
                case Operation.DELETE:
                    text = text.Append("-").Append(aDiff.Text.Length.ToString()).Append("\t");
                    break;
                case Operation.EQUAL:
                    text = text.Append("=").Append(aDiff.Text.Length.ToString()).Append("\t");
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
                case Operation.INSERT:
                    insertions += aDiff.Text.Length;
                    break;
                case Operation.DELETE:
                    deletions += aDiff.Text.Length;
                    break;
                case Operation.EQUAL:
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
