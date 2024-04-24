namespace Rope.Compare;

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

public static class DiffExtensions
{

    /// <summary>
    /// Convert a Diff list into a pretty HTML report.
    /// </summary>
    /// <param name="diffs">List of Diff objects.</param>
    /// <returns>HTML string representation of the diff.</returns>
    [Pure]
    public static Rope<char> ToHtmlReport(this IEnumerable<Diff<char>> diffs)
    {
        var html = Rope<char>.Empty;
        foreach (Diff<char> aDiff in diffs)
        {
            var text = aDiff.Text
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
    public static Rope<char> ToSource(this IEnumerable<Diff<char>> diffs)
    {
        var text = Rope<char>.Empty;
        foreach (var aDiff in diffs)
        {
            if (aDiff.Operation != Operation.INSERT)
            {
                text = text.AddRange(aDiff.Text);
            }
        }

        return text;
    }

    /// <summary>
    /// Compute and return the destination text from a list of diff operations.
    /// Includes all equalities and insertions.
    /// </summary>
    /// <param name="diffs">List of Diff objects</param>
    /// <returns>Destination text.</returns>
    [Pure]
    public static Rope<char> ToDestination(this IEnumerable<Diff<char>> diffs)
    {
        var text = Rope<char>.Empty;
        foreach (var aDiff in diffs)
        {
            if (aDiff.Operation != Operation.DELETE)
            {
                text = text.AddRange(aDiff.Text);
            }
        }

        return text;
    }

    /// <summary>
    /// Compute and return the source and destination texts from a list of diff operations.
    /// Includes all equalities and deletions in the source, and all equalities and insertions
    /// in the destination.
    /// </summary>
    /// <param name="diffs">List of Diff objects</param>
    /// <returns>Both the Source and Destination text.</returns>
    public static (Rope<char> Source, Rope<char> Destination) ToSourceAndDestination(this IEnumerable<Diff<char>> diffs)
    {
        var text = (Rope<char>.Empty, Rope<char>.Empty);
        foreach (var myDiff in diffs)
        {
            if (myDiff.Operation != Operation.INSERT)
            {
                text.Item1 = text.Item1 + myDiff.Text;
            }

            if (myDiff.Operation != Operation.DELETE)
            {
                text.Item2 += myDiff.Text;
            }
        }

        return text;
    }

    /// <summary>
    /// Given a location in text1, compute and return the equivalent location in
    /// text2.
    /// e.g. "The cat" vs "The big cat", 1->1, 5->8
    /// </summary>
    /// <param name="diffs">List of Diff objects.</param>
    /// <param name="sourceIndex">Location within text1.</param>
    /// <returns>Location within text2.</returns>
    [Pure]
    public static long TranslateToDestinationIndex(this IEnumerable<Diff<char>> diffs, long sourceIndex)
    {
        long chars1 = 0;
        long chars2 = 0;
        long last_chars1 = 0;
        long last_chars2 = 0;
        Diff<char> lastDiff = default;
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
    /// required to transform text1 into text2.
    /// E.g. =3\t-2\t+ing  -> Keep 3 chars, delete 2 chars, insert 'ing'.
    /// Operations are tab-separated. Inserted text is escaped using %xx
    /// Url encoding notation.
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
    public static long CalculateEditDistance(this IEnumerable<Diff<char>> diffs)
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
