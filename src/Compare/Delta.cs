
namespace Rope.Compare;

using System;
using System.Diagnostics.Contracts;

public static class Delta
{
    /// <summary>
    /// Computes the full diff, given an encoded string which describes the operations required 
    /// to transform source list into destination list (the delta), 
    /// and the original list the delta was created from.
    /// </summary>
    /// <param name="delta">The delta text to be parsed.</param>
    /// <param name="original">Original list the delta was created from and the diffs will be applied to.</param>
    /// <returns>Rope of Diff objects or null if invalid.</returns>
    /// <exception cref="ArgumentException">If invalid input.</exception>
    [Pure]
    public static Rope<Diff<char>> Parse(this Rope<char> delta, Rope<char> original)
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
                    diffs = diffs.Add(new Diff<char>(Operation.Insert, param));
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
                        text = original.Slice(pointer, n);
                        pointer += n;
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        throw new ArgumentException($"Delta length ({pointer}) larger than source text length ({original.Length}).", e);
                    }

                    if (token[0] == '=')
                    {
                        diffs = diffs.Add(new Diff<char>(Operation.Equal, text));
                    }
                    else
                    {
                        diffs = diffs.Add(new Diff<char>(Operation.Delete, text));
                    }

                    break;
                default:
                    // Anything else is an error.
                    throw new ArgumentException($"Invalid diff operation in diff_fromDelta: {token[0]}");
            }
        }

        if (pointer != original.Length)
        {
            throw new ArgumentException("Delta length (" + pointer + ") smaller than source text length (" + original.Length + ").");
        }

        return diffs;
    }

    /// <summary>
    /// Computes the full diff, given an encoded string which describes the operations required 
    /// to transform source list into destination list (the delta), 
    /// and the original list the delta was created from.
    /// </summary>
    /// <param name="delta">The delta text to be parsed.</param>
    /// <param name="original">Original list the delta was created from and the diffs will be applied to.</param>
    /// <returns>Rope of Diff objects or null if invalid.</returns>
    /// <exception cref="ArgumentException">If invalid input.</exception>
    [Pure]
    public static Rope<Diff<T>> Parse<T>(this Rope<char> delta, Rope<T> original, Func<Rope<char>, T> parseItem) where T : IEquatable<T>
    {
        var diffs = Rope<Diff<T>>.Empty;

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
                    var x = param.DiffDecode(parseItem);
                    diffs = diffs.Add(new Diff<T>(Operation.Insert, x));
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

                    Rope<T> text;
                    try
                    {
                        text = original.Slice(pointer, n);
                        pointer += n;
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        throw new ArgumentException($"Delta length ({pointer}) larger than source text length ({original.Length}).", e);
                    }

                    if (token[0] == '=')
                    {
                        diffs = diffs.Add(new Diff<T>(Operation.Equal, text));
                    }
                    else
                    {
                        diffs = diffs.Add(new Diff<T>(Operation.Delete, text));
                    }

                    break;
                default:
                    // Anything else is an error.
                    throw new ArgumentException($"Invalid diff operation in diff_fromDelta: {token[0]}");
            }
        }

        if (pointer != original.Length)
        {
            throw new ArgumentException("Delta length (" + pointer + ") smaller than source text length (" + original.Length + ").");
        }

        return diffs;
    }
}
