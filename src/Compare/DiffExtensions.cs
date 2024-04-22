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
    public static Rope<char> ToHtmlReport(this IEnumerable<Diff> diffs)
    {
        var html = Rope<char>.Empty;
        foreach (Diff aDiff in diffs)
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
    /// Compute and return the source text(all equalities and deletions).
    /// </summary>
    /// <param name="diffs">List of Diff objects</param>
    /// <returns>Source text.</returns>
    [Pure]
    public static Rope<char> ToSourceText(this IEnumerable<Diff> diffs)
    {
        var text = Rope<char>.Empty;
        foreach (Diff aDiff in diffs)
        {
            if (aDiff.Operation != Operation.INSERT)
            {
                text = text.AddRange(aDiff.Text);
            }
        }

        return text;
    }

    /// <summary>
    /// Compute and return the destination text (all equalities and insertions).
    /// </summary>
    /// <param name="diffs">List of Diff objects</param>
    /// <returns>Destination text.</returns>
    [Pure]
    public static Rope<char> ToDestinationText(this IEnumerable<Diff> diffs)
    {
        var text = Rope<char>.Empty;
        foreach (Diff aDiff in diffs)
        {
            if (aDiff.Operation != Operation.DELETE)
            {
                text = text.AddRange(aDiff.Text);
            }
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
    public static long CalculateEditDistance(this IEnumerable<Diff> diffs)
    {
        long levenshtein = 0;
        long insertions = 0;
        long deletions = 0;
        foreach (Diff aDiff in diffs)
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
