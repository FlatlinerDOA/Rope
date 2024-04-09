using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Rope;

/*
* Diff Match and Patch
* Copyright 2018 The diff-match-patch Authors.
* https://github.com/google/diff-match-patch
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
*/

internal static class CompatibilityExtensions
{
    // JScript splice function
    [Obsolete("Use pure Splice")]
    public static List<T> Splice<T>(this List<T> input, int start, int count, params T[] objects)
    {
        List<T> deletedRange = input.GetRange(start, count);
        input.RemoveRange(start, count);
        input.InsertRange(start, objects);
        return deletedRange;
    }

    [Pure]
    public static (Rope<T> Deleted, Rope<T> Result) Splice<T>(this Rope<T> input, int start, int count, params T[] objects) where T : IEquatable<T>
    {
        var deletedRange = input.Slice(start, count);
        input = input.RemoveRange(start, count);
        input = input.InsertRange(start, new Rope<T>(objects.AsMemory()));
        return (deletedRange, input);
    }

    // Java substring function
    [Pure]
    public static string JavaSubstring(this string s, int begin, int end)
    {
        return s.Substring(begin, end - begin);
    }

    [Pure]
    public static Rope<char> JavaSubstring(this Rope<char> s, long begin, long end) => s.Slice(begin, end - begin);

    [Pure]
    public static Rope<char> Replace(this Rope<char> s, string replace, string with) => s.Replace(replace.ToRope(), with.ToRope());
    
    [Pure]
    public static Rope<char> Append(this Rope<char> s, string append) => s.AddRange(append.ToRope());

    [Pure]
    public static ReadOnlyMemory<char> JavaSubstring(this ReadOnlyMemory<char> s, int begin, int end) => s.Slice(begin, end - begin);

    [Pure]
    public static Rope<T> Concat<T>(this Rope<T> source, Rope<T> append) where T : IEquatable<T> => source.AddRange(append);

    [Pure]
    public static ReadOnlyMemory<T> Concat<T>(this ReadOnlyMemory<T> source, ReadOnlyMemory<T> append)
    {
        var mem = new T[source.Length + append.Length];
        source.CopyTo(mem[0..source.Length]);
        append.CopyTo(mem[source.Length..]);
        return mem.AsMemory();
    }
}

/**-
 * The data structure representing a diff is a List of Diff objects:
 * {Diff(Operation.DELETE, "Hello"), Diff(Operation.INSERT, "Goodbye"),
 *  Diff(Operation.EQUAL, " world.")}
 * which means: delete "Hello", add "Goodbye" and keep " world."
 */
public enum Operation
{
    DELETE, INSERT, EQUAL
}


/**
 * Class representing one diff operation.
 */
public readonly record struct Diff(Operation Operation, Rope<char> Text)
{
    public Diff(Operation operation, string text) : this(operation, text.AsMemory())
    {
    }

    public Diff(Operation operation, ReadOnlyMemory<char> text) : this(operation, new Rope<char>(text))
    {
    }

    public Diff WithOperation(Operation op) => this with { Operation = op };
    public Diff WithText(Rope<char> text) => this with { Text = text };

    public Diff Append(Rope<char> text) => this with { Text = this.Text.AddRange(text) };
    public Diff Prepend(Rope<char> text) => this with { Text = text.AddRange(this.Text) };
}


/**
 * Class representing one patch operation.
 */
public sealed record class Patch()
{
    public Rope<Diff> Diffs { get; init; } = Rope<Diff>.Empty;
    public long Start1 { get; init; }
    public long Start2 { get; init; }
    public long Length1 { get; init; }
    public long Length2 { get; init; }

    /**
     * Emulate GNU diff's format.
     * Header: @@ -382,8 +481,9 @@
     * Indices are printed as 1-based, not 0-based.
     * @return The GNU diff string.
     */
    public override string ToString()
    {
        string coords1, coords2;
        if (this.Length1 == 0)
        {
            coords1 = this.Start1 + ",0";
        }
        else if (this.Length1 == 1)
        {
            coords1 = Convert.ToString(this.Start1 + 1);
        }
        else
        {
            coords1 = (this.Start1 + 1) + "," + this.Length1;
        }
        if (this.Length2 == 0)
        {
            coords2 = this.Start2 + ",0";
        }
        else if (this.Length2 == 1)
        {
            coords2 = Convert.ToString(this.Start2 + 1);
        }
        else
        {
            coords2 = (this.Start2 + 1) + "," + this.Length2;
        }

        var text = Rope<char>.Empty;
        text = text.Append("@@ -").Append(coords1).Append(" +").Append(coords2)
            .Append(" @@\n");
        // Escape the body of the patch with %xx notation.
        foreach (Diff aDiff in this.Diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.INSERT:
                    text = text.Add('+');
                    break;
                case Operation.DELETE:
                    text = text.Add('-');
                    break;
                case Operation.EQUAL:
                    text = text.Add(' ');
                    break;
            }

            text = text.AddRange(diff_match_patch.encodeURI(aDiff.Text)).Append("\n");
        }

        return text.ToString();
    }
}

/**
 * Class containing the diff, match and patch methods.
 * Also Contains the behaviour settings.
 */
public class diff_match_patch
{
    // Defaults.
    // Set these on your diff_match_patch instance to override the defaults.

    // Number of seconds to map a diff before giving up (0 for infinity).
    public float Diff_Timeout = 1.0f;
    // Cost of an empty edit operation in terms of edit characters.
    public short Diff_EditCost = 4;
    // At what point is no match declared (0.0 = perfection, 1.0 = very loose).
    public float Match_Threshold = 0.5f;
    // How far to search for a match (0 = exact location, 1000+ = broad match).
    // A match this many characters away from the expected location will add
    // 1.0 to the score (0.0 is a perfect match).
    public int Match_Distance = 1000;
    // When deleting a large block of text (over ~64 characters), how close
    // do the contents have to be to match the expected contents. (0.0 =
    // perfection, 1.0 = very loose).  Note that Match_Threshold controls
    // how closely the end points of a delete need to match.
    public float Patch_DeleteThreshold = 0.5f;
    // Chunk size for context length.
    public short Patch_Margin = 4;

    // The number of bits in an int.
    private short Match_MaxBits = 32;


    private sealed class Deadline
    {
        private readonly TimeSpan timeout;

        public Deadline(float timeoutSeconds) : this(timeoutSeconds <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(timeoutSeconds))
        {
        }

        public Deadline(TimeSpan timeout)
        {
            this.timeout = timeout;
            if (timeout != TimeSpan.Zero)
            {
                var source = new CancellationTokenSource();
                source.CancelAfter((int)timeout.TotalMilliseconds);
                this.Cancellation = source.Token;
            }
            else
            {
                this.Cancellation = CancellationToken.None;
            }
        }

        public CancellationToken Cancellation { get; }
    }

    //  DIFF FUNCTIONS


    /**
     * Find the differences between two texts.
     * Run a faster, slightly less optimal diff.
     * This method allows the 'checklines' of diff_main() to be optional.
     * Most of the time checklines is wanted, so default to true.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @return List of Diff objects.
     */
    public Rope<Diff> diff_main(string text1, string text2) => diff_main(text1, text2, true);

    /**
     * Find the differences between two texts.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param checklines Speedup flag.  If false, then don't run a
     *     line-level diff first to identify the changed areas.
     *     If true, then run a faster slightly less optimal diff.
     * @return List of Diff objects.
     */
     public Rope<Diff> diff_main(string text1, string text2, bool checklines)  => diff_main(text1.ToRope(), text2.ToRope(), checklines);
    
    public Rope<Diff> diff_main(Rope<char> text1, Rope<char> text2, bool checklines)
    {
        // Set a deadline by which time the diff must be complete.
        var deadline = new Deadline(this.Diff_Timeout);
        return diff_main(text1, text2, checklines, deadline.Cancellation);
    }

    /**
     * Find the differences between two texts.  Simplifies the problem by
     * stripping any common prefix or suffix off the texts before diffing.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param checklines Speedup flag.  If false, then don't run a
     *     line-level diff first to identify the changed areas.
     *     If true, then run a faster slightly less optimal diff.
     * @param deadline Time when the diff should be complete by.  Used
     *     internally for recursive calls.  Users should set DiffTimeout
     *     instead.
     * @return List of Diff objects.
     */
    private Rope<Diff> diff_main(Rope<char> text1, Rope<char> text2, bool checklines, CancellationToken cancel)
    {
        // Check for null inputs not needed since null can't be passed in C#.
        // Check for equality (speedup).
        if (text1.Equals(text2))
        {
            if (text1.Length != 0)
            {
                return new[] { new Diff(Operation.EQUAL, text1) };
            }

            return Rope<Diff>.Empty;
        }

        // Trim off common prefix (speedup).
        int commonlength = diff_commonPrefix(text1, text2);
        var commonprefix = text1.Slice(0, commonlength);
        text1 = text1.Slice(commonlength);
        text2 = text2.Slice(commonlength);

        // Trim off common suffix (speedup).
        commonlength = diff_commonSuffix(text1, text2);
        var commonsuffix = text1.Slice(text1.Length - commonlength);
        text1 = text1.Slice(0, text1.Length - commonlength);
        text2 = text2.Slice(0, text2.Length - commonlength);

        // Compute the diff on the middle block.
        var diffs = diff_compute(text1, text2, checklines, cancel);

        // Restore the prefix and suffix.
        if (commonprefix.Length != 0)
        {
            diffs = diffs.Prepend(new Diff(Operation.EQUAL, commonprefix));
        }

        if (commonsuffix.Length != 0)
        {
            diffs = diffs.Append(new Diff(Operation.EQUAL, commonsuffix));
        }

        return diff_cleanupMerge_pure(diffs);
    }

    /**
     * Find the differences between two texts.  Assumes that the texts do not
     * have any common prefix or suffix.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param checklines Speedup flag.  If false, then don't run a
     *     line-level diff first to identify the changed areas.
     *     If true, then run a faster slightly less optimal diff.
     * @param cancel Time when the diff should be complete by.
     * @return List of Diff objects.
     */
    private IEnumerable<Diff> diff_compute(Rope<char> text1, Rope<char> text2, bool checklines, CancellationToken cancel)
    {
        if (text1.Length == 0)
        {
            // Just add some text (speedup).
            yield return new Diff(Operation.INSERT, text2);
            yield break;
        }

        if (text2.Length == 0)
        {
            // Just delete some text (speedup).
            yield return new Diff(Operation.DELETE, text1);
            yield break;
        }

        var longtext = text1.Length > text2.Length ? text1 : text2;
        var shorttext = text1.Length > text2.Length ? text2 : text1;
        var i = longtext.IndexOf(shorttext);
        if (i != -1)
        {
            // Shorter text is inside the longer text (speedup).
            Operation op = (text1.Length > text2.Length) ? Operation.DELETE : Operation.INSERT;
            yield return new Diff(op, longtext.Slice(0, i));
            yield return new Diff(Operation.EQUAL, shorttext);
            yield return new Diff(op, longtext.Slice(i + shorttext.Length));
            yield break;
        }

        if (shorttext.Length == 1)
        {
            // Single character string.
            // After the previous speedup, the character can't be an equality.
            yield return new Diff(Operation.DELETE, text1);
            yield return new Diff(Operation.INSERT, text2);
            yield break;
        }

        // Check to see if the problem can be split in two.
        var hm = diff_halfMatch(text1, text2);
        if (hm != null)
        {
            // A half-match was found, sort out the return data.
            var text1_a = hm[0];
            var text1_b = hm[1];
            var text2_a = hm[2];
            var text2_b = hm[3];
            var mid_common = hm[4];

            // Send both pairs off for separate processing.
            var diffs_a = diff_main(text1_a, text2_a, checklines, cancel);
            foreach (var a in diffs_a)
            {
                yield return a;
            }

            yield return new Diff(Operation.EQUAL, mid_common);

            var diffs_b = diff_main(text1_b, text2_b, checklines, cancel);
            foreach (var b in diffs_b)
            {
                yield return b;
            }

            yield break;
        }

        if (checklines && text1.Length > 100 && text2.Length > 100)
        {
            foreach (var line in diff_lineMode(text1, text2, cancel))
            {
                yield return line;
            }

            yield break;
        }

        foreach (var d in diff_bisect(text1, text2, cancel))
        {
            yield return d;
        }
    }

    /**
     * Do a quick line-level diff on both strings, then rediff the parts for
     * greater accuracy.
     * This speedup can produce non-minimal diffs.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param deadline Time when the diff should be complete by.
     * @return List of Diff objects.
     */
    private Rope<Diff> diff_lineMode(Rope<char> text1, Rope<char> text2, CancellationToken cancel)
    {
        // Scan the text on a line-by-line basis first.
        (text1, text2, var linearray) = diff_linesToChars_pure(text1, text2);
        var diffsx = diff_main(text1, text2, false, cancel);

        // Convert the diff back to original text.
        diffsx = diff_charsToLines_pure(diffsx, linearray);

        // Eliminate freak matches (e.g. blank lines)
        var diffs = diff_cleanupSemantic_pure(diffsx);

        // Rediff any replacement blocks, this time character-by-character.
        // Add a dummy entry at the end.
        diffs = diffs.Add(new Diff(Operation.EQUAL, string.Empty));
        int pointer = 0;
        int count_delete = 0;
        int count_insert = 0;
        var text_delete = Rope<char>.Empty;
        var text_insert = Rope<char>.Empty;
        while (pointer < diffs.Count)
        {
            switch (diffs[pointer].Operation)
            {
                case Operation.INSERT:
                    count_insert++;
                    text_insert = text_insert.AddRange(diffs[pointer].Text);
                    break;
                case Operation.DELETE:
                    count_delete++;
                    text_delete = text_delete.AddRange(diffs[pointer].Text);
                    break;
                case Operation.EQUAL:
                    // Upon reaching an equality, check for prior redundancies.
                    if (count_delete >= 1 && count_insert >= 1)
                    {
                        // Delete the offending records and add the merged ones.
                        diffs = diffs.RemoveRange(pointer - count_delete - count_insert, count_delete + count_insert);
                        pointer = pointer - count_delete - count_insert;
                        var subDiff = this.diff_main(text_delete, text_insert, false, cancel);
                        diffs = diffs.InsertRange(pointer, subDiff);
                        pointer = pointer + subDiff.Count;
                    }
                    count_insert = 0;
                    count_delete = 0;
                    text_delete = Rope<char>.Empty;
                    text_insert = Rope<char>.Empty;
                    break;
            }
            pointer++;
        }

        diffs = diffs.RemoveAt(diffs.Count - 1);  // Remove the dummy entry at the end.
        return diffs;
    }

    /**
     * Find the 'middle snake' of a diff, split the problem in two
     * and return the recursively constructed diff.
     * See Myers 1986 paper: An O(ND) Difference Algorithm and Its Variations.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param deadline Time at which to bail if not yet complete.
     * @return List of Diff objects.
     */
    protected IReadOnlyList<Diff> diff_bisect(string text1, string text2, CancellationToken cancel) => diff_bisect(text1.AsMemory(), text2.AsMemory(), cancel);
    protected Rope<Diff> diff_bisect(Rope<char> text1, Rope<char> text2, CancellationToken cancel)
    {
        // Cache the text lengths to prevent multiple calls.
        var text1_length = text1.Length;
        var text2_length = text2.Length;
        var max_d = (text1_length + text2_length + 1) / 2;
        var v_offset = max_d;
        var v_length = 2 * max_d;
        var v1 = new long[v_length];
        var v2 = new long[v_length];
        for (var x = 0; x < v_length; x++)
        {
            v1[x] = -1;
            v2[x] = -1;
        }

        v1[v_offset + 1] = 0;
        v2[v_offset + 1] = 0;
        var delta = text1_length - text2_length;
        // If the total number of characters is odd, then the front path will
        // collide with the reverse path.
        bool front = (delta % 2 != 0);
        // Offsets for start and end of k loop.
        // Prevents mapping of space beyond the grid.
        long k1start = 0;
        long k1end = 0;
        long k2start = 0;
        long k2end = 0;
        for (long d = 0; d < max_d; d++)
        {
            // Bail out if deadline is reached.
            if (cancel.IsCancellationRequested)
            {
                break;
            }

            // Walk the front path one step.
            for (var k1 = -d + k1start; k1 <= d - k1end; k1 += 2)
            {
                var k1_offset = v_offset + k1;
                long x1;
                if (k1 == -d || k1 != d && v1[k1_offset - 1] < v1[k1_offset + 1])
                {
                    x1 = v1[k1_offset + 1];
                }
                else
                {
                    x1 = v1[k1_offset - 1] + 1;
                }
                
                long y1 = x1 - k1;
                while (x1 < text1_length && y1 < text2_length && text1[x1] == text2[y1])
                {
                    x1++;
                    y1++;
                }
                v1[k1_offset] = x1;
                if (x1 > text1_length)
                {
                    // Ran off the right of the graph.
                    k1end += 2;
                }
                else if (y1 > text2_length)
                {
                    // Ran off the bottom of the graph.
                    k1start += 2;
                }
                else if (front)
                {
                    var k2_offset = v_offset + delta - k1;
                    if (k2_offset >= 0 && k2_offset < v_length && v2[k2_offset] != -1)
                    {
                        // Mirror x2 onto top-left coordinate system.
                        var x2 = text1_length - v2[k2_offset];
                        if (x1 >= x2)
                        {
                            // Overlap detected.
                            return diff_bisectSplit(text1, text2, x1, y1, cancel);
                        }
                    }
                }
            }

            // Walk the reverse path one step.
            for (var k2 = -d + k2start; k2 <= d - k2end; k2 += 2)
            {
                var k2_offset = v_offset + k2;
                long x2;
                if (k2 == -d || k2 != d && v2[k2_offset - 1] < v2[k2_offset + 1])
                {
                    x2 = v2[k2_offset + 1];
                }
                else
                {
                    x2 = v2[k2_offset - 1] + 1;
                }
                
                var y2 = x2 - k2;
                while (x2 < text1_length && y2 < text2_length && text1[text1_length - x2 - 1] == text2[text2_length - y2 - 1])
                {
                    x2++;
                    y2++;
                }

                v2[k2_offset] = x2;
                if (x2 > text1_length)
                {
                    // Ran off the left of the graph.
                    k2end += 2;
                }
                else if (y2 > text2_length)
                {
                    // Ran off the top of the graph.
                    k2start += 2;
                }
                else if (!front)
                {
                    var k1_offset = v_offset + delta - k2;
                    if (k1_offset >= 0 && k1_offset < v_length && v1[k1_offset] != -1)
                    {
                        var x1 = v1[k1_offset];
                        var y1 = v_offset + x1 - k1_offset;
                        
                        // Mirror x2 onto top-left coordinate system.
                        x2 = text1_length - v2[k2_offset];
                        if (x1 >= x2)
                        {
                            // Overlap detected.
                            return diff_bisectSplit(text1, text2, x1, y1, cancel);
                        }
                    }
                }
            }
        }

        // Diff took too long and hit the deadline or
        // number of diffs equals number of characters, no commonality at all.
        return new Rope<Diff>(new[] { new Diff(Operation.DELETE, text1), new Diff(Operation.INSERT, text2) });
    }

    /**
     * Given the location of the 'middle snake', split the diff in two parts
     * and recurse.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param x Index of split point in text1.
     * @param y Index of split point in text2.
     * @param deadline Time at which to bail if not yet complete.
     * @return LinkedList of Diff objects.
     */
    private Rope<Diff> diff_bisectSplit(Rope<char> text1, Rope<char> text2, long x, long y, CancellationToken cancel)
    {
        var text1a = text1.Slice(0, x);
        var text2a = text2.Slice(0, y);
        var text1b = text1.Slice(x);
        var text2b = text2.Slice(y);

        // Compute both diffs serially.
        var diffs = diff_main(text1a, text2a, false, cancel);
        var diffsb = diff_main(text1b, text2b, false, cancel);
        return diffs.Concat(diffsb);
    }

    [Obsolete("diff_linesToChars_pure")]
    protected Object[] diff_linesToChars(string text1, string text2)
    {
        List<string> lineArray = new List<string>();
        Dictionary<string, int> lineHash = new Dictionary<string, int>();
        // e.g. linearray[4] == "Hello\n"
        // e.g. linehash.get("Hello\n") == 4

        // "\x00" is a valid character, but various debuggers don't like it.
        // So we'll insert a junk entry to avoid generating a null character.
        lineArray.Add(string.Empty);

        // Allocate 2/3rds of the space for text1, the rest for text2.
        string chars1 = diff_linesToCharsMunge(text1, lineArray, lineHash, 40000);
        string chars2 = diff_linesToCharsMunge(text2, lineArray, lineHash, 65535);
        return new Object[] { chars1, chars2, lineArray };
    }

    [Obsolete("diff_linesToCharsMunge_pure")]
    private string diff_linesToCharsMunge(string text, List<string> lineArray, Dictionary<string, int> lineHash, int maxLines)
    {
        int lineStart = 0;
        int lineEnd = -1;
        string line;
        StringBuilder chars = new StringBuilder();
        // Walk the text, pulling out a Substring for each line.
        // text.split('\n') would would temporarily double our memory footprint.
        // Modifying text would create many large strings to garbage collect.
        while (lineEnd < text.Length - 1)
        {
            lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd == -1)
            {
                lineEnd = text.Length - 1;
            }
            line = text.JavaSubstring(lineStart, lineEnd + 1);

            if (lineHash.ContainsKey(line))
            {
                chars.Append(((char)(int)lineHash[line]));
            }
            else
            {
                if (lineArray.Count == maxLines)
                {
                    // Bail out at 65535 because char 65536 == char 0.
                    line = text.Substring(lineStart);
                    lineEnd = text.Length;
                }
                lineArray.Add(line);
                lineHash.Add(line, lineArray.Count - 1);
                chars.Append(((char)(lineArray.Count - 1)));
            }

            lineStart = lineEnd + 1;
        }
        return chars.ToString();
    }

    protected (string Text1Encoded, string Text2Encoded, List<string> Lines) diff_linesToChars_pure(string text1, string text2)
    {
        var (a, b, c) = diff_linesToChars_pure(text1.ToRope(), text2.ToRope());
        return (a.ToString(), b.ToString(), c.Select(f => f.ToString()).ToList());
    }

    /**
     * Split two texts into a list of strings.  Reduce the texts to a string of
     * hashes where each Unicode character represents one line.
     * @param text1 First string.
     * @param text2 Second string.
     * @return Three element Object array, containing the encoded text1, the
     *     encoded text2 and the List of unique strings.  The zeroth element
     *     of the List of unique strings is intentionally blank.
     */
    protected (Rope<char> Text1Encoded, Rope<char> Text2Encoded, Rope<Rope<char>> Lines) diff_linesToChars_pure(Rope<char> text1, Rope<char> text2)
    {
        var lineArray = Rope<Rope<char>>.Empty;
        var lineHash = ImmutableDictionary<Rope<char>, int>.Empty;
        // e.g. linearray[4] == "Hello\n"
        // e.g. linehash.get("Hello\n") == 4

        // "\x00" is a valid character, but various debuggers don't like it.
        // So we'll insert a junk entry to avoid generating a null character.
        lineArray = lineArray.Add(Rope<char>.Empty);

        // Allocate 2/3rds of the space for text1, the rest for text2.
        (var chars1, lineArray, lineHash) = diff_linesToCharsMunge_pure(text1, lineArray, lineHash, 40000);
        (var chars2, lineArray, lineHash) = diff_linesToCharsMunge_pure(text2, lineArray, lineHash, 65535);
        return (chars1, chars2, lineArray);
    }

    /**
     * Split a text into a list of strings.  Reduce the texts to a string of
     * hashes where each Unicode character represents one line.
     * @param text String to encode.
     * @param lineArray List of unique strings.
     * @param lineHash Map of strings to indices.
     * @param maxLines Maximum length of lineArray.
     * @return Encoded string.
     */
    private (Rope<char> Chars, Rope<Rope<char>> LineArray, ImmutableDictionary<Rope<char>, int> LineHash) diff_linesToCharsMunge_pure(Rope<char> text, Rope<Rope<char>> lineArray, ImmutableDictionary<Rope<char>, int> lineHash, int maxLines)
    {
        long lineStart = 0;
        long lineEnd = -1;
        var line = Rope<char>.Empty;
        var chars = Rope<char>.Empty;

        // Walk the text, pulling out a Substring for each line.
        // text.split('\n') would would temporarily double our memory footprint.
        // Modifying text would create many large strings to garbage collect.
        while (lineEnd < text.Length - 1)
        {
            lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd == -1)
            {
                lineEnd = text.Length - 1;
            }

            line = text.JavaSubstring(lineStart, lineEnd + 1);
            if (lineHash.ContainsKey(line))
            {
                chars = chars.Add((char)(int)lineHash[line]);
            }
            else
            {
                if (lineArray.Count == maxLines)
                {
                    // Bail out at 65535 because char 65536 == char 0.
                    line = text.Slice(lineStart);
                    lineEnd = text.Length;
                }

                lineArray = lineArray.Add(line);
                lineHash = lineHash.Add(line, lineArray.Count - 1);
                chars = chars.Add((char)(lineArray.Count - 1));
            }

            lineStart = lineEnd + 1;
        }

        return (chars, lineArray, lineHash);
    }

    /**
     * Rehydrate the text in a diff from a string of line hashes to real lines
     * of text.
     * @param diffs List of Diff objects.
     * @param lineArray List of unique strings.
     */
    [Obsolete("use diff_charsToLines_pure", true)]
    protected void diff_charsToLines(IList<Diff> diffs, IReadOnlyList<string> lineArray)
    {
        StringBuilder text;
        for (var d = 0; d < diffs.Count; d++)
        {
            var diff = diffs[d];
            text = new StringBuilder();
            for (int j = 0; j < diff.Text.Length; j++)
            {
                text.Append(lineArray[diff.Text[j]]);
            }

            diffs[d] = new(diff.Operation, text.ToString().AsMemory());
        }
    }

    [Pure]
    protected Rope<Diff> diff_charsToLines_pure(Rope<Diff> diffs, Rope<Rope<char>> lineArray)
    {
        var text = Rope<char>.Empty;
        var result = Rope<Diff>.Empty;
        foreach (Diff diff in diffs)
        {
            text = Rope<char>.Empty;
            for (int j = 0; j < diff.Text.Length; j++)
            {
                text = text.AddRange(lineArray[(int)diff.Text[j]]);
            }

            result = result.Add(new(diff.Operation, text));
        }

        return result;
    }

    [Pure]
    public int diff_commonPrefix(string text1, string text2) => diff_commonPrefix(text1.ToRope(), text2.ToRope());

    /**
     * Determine the common prefix of two strings.
     * @param text1 First string.
     * @param text2 Second string.
     * @return The number of characters common to the start of each string.
     */
    [Pure]
    public int diff_commonPrefix(Rope<char> text1, Rope<char> text2) => (int)text1.CommonPrefixLength(text2);
    // {
    // 	  // Performance analysis: https://neil.fraser.name/news/2007/10/09/
    //       var n = Math.Min(text1.Length, text2.Length);
    //       for (int i = 0; i < n; i++) {
    //         if (text1[i] != text2[i]) {
    //           return i;
    //         }
    //       }

    //       return (int)n;
    // }

    [Pure]
    public int diff_commonSuffix(string text1, string text2) => diff_commonSuffix(text1.ToRope(), text2.ToRope());

    /**
     * Determine the common suffix of two strings.
     * @param text1 First string.
     * @param text2 Second string.
     * @return The number of characters common to the end of each string.
     */
    [Pure]
    public int diff_commonSuffix(Rope<char> text1, Rope<char> text2) => (int)text1.CommonSuffixLength(text2);
    // {
    // 	// Performance analysis: https://neil.fraser.name/news/2007/10/09/
    // 	int text1_length = (int)text1.Length;
    // 	int text2_length = (int)text2.Length;
    // 	int n = Math.Min(text1_length, text2_length);
    // 	for (int i = 1; i <= n; i++)
    // 	{
    // 		if (text1[text1_length - i] != text2[text2_length - i])
    // 		{
    // 			return i - 1;
    // 		}
    // 	}

    //  	return n;
    // }

    [Pure]
    protected int diff_commonOverlap(string text1, string text2) => diff_commonOverlap(text1.ToRope(), text2.ToRope());
    

    /**
     * Determine if the suffix of one string is the prefix of another.
     * @param text1 First string.
     * @param text2 Second string.
     * @return The number of characters common to the end of the first
     *     string and the start of the second string.
     */
    [Pure]
    protected int diff_commonOverlap(Rope<char> text1, Rope<char> text2)
    {
        // Cache the text lengths to prevent multiple calls.
        var text1_length = text1.Length;
        var text2_length = text2.Length;
        // Eliminate the null case.
        if (text1_length == 0 || text2_length == 0)
        {
            return 0;
        }
        // Truncate the longer string.
        if (text1_length > text2_length)
        {
            text1 = text1.Slice(text1_length - text2_length);
        }
        else if (text1_length < text2_length)
        {
            text2 = text2.Slice(0, text1_length);
        }

        var text_length = Math.Min(text1_length, text2_length);
        // Quick check for the worst case.
        if (text1 == text2)
        {
            return (int)text_length;
        }

        // Start by looking for a single character match
        // and increase length until no match is found.
        // Performance analysis: https://neil.fraser.name/news/2010/11/04/
        long best = 0;
        long length = 1;
        while (true)
        {
            var pattern = text1.Slice(text_length - length);
            var found = text2.IndexOf(pattern);
            if (found == -1)
            {
                return (int)best;
            }

            length += found;
            if (found == 0 || text1.Slice(text_length - length) == text2.Slice(0, length))
            {
                best = length;
                length++;
            }
        }
    }

    /**
     * Do the two texts share a Substring which is at least half the length of
     * the longer text?
     * This speedup can produce non-minimal diffs.
     * @param text1 First string.
     * @param text2 Second string.
     * @return Five element String array, containing the prefix of text1, the
     *     suffix of text1, the prefix of text2, the suffix of text2 and the
     *     common middle.  Or null if there was no match.
     */
     
    [Pure] 
    protected string[]? diff_halfMatch(string text1, string text2) => diff_halfMatch(text1.ToRope(), text2.ToRope())?.Select(p => p.ToString()).ToArray();

    [Pure]
    protected Rope<char>[]? diff_halfMatch(Rope<char> text1, Rope<char> text2)
    {
        if (this.Diff_Timeout <= 0)
        {
            // Don't risk returning a non-optimal diff if we have unlimited time.
            return null;
        }

        var longtext = text1.Length > text2.Length ? text1 : text2;
        var shorttext = text1.Length > text2.Length ? text2 : text1;
        if (longtext.Length < 4 || shorttext.Length * 2 < longtext.Length)
        {
            return null;  // Pointless.
        }

        // First check if the second quarter is the seed for a half-match.
        var hm1 = diff_halfMatchI(longtext, shorttext, (longtext.Length + 3) / 4);
        // Check again based on the third quarter.
        var hm2 = diff_halfMatchI(longtext, shorttext, (longtext.Length + 1) / 2);
        Rope<char>[]? hm;
        if (hm1 == null && hm2 == null)
        {
            return null;
        }
        else if (hm2 == null)
        {
            hm = hm1;
        }
        else if (hm1 == null)
        {
            hm = hm2;
        }
        else
        {
            // Both matched.  Select the longest.
            hm = hm1[4].Length > hm2[4].Length ? hm1 : hm2;
        }

        // A half-match was found, sort out the return data.
        if (text1.Length > text2.Length)
        {
            return hm;
            //return new string[]{hm[0], hm[1], hm[2], hm[3], hm[4]};
        }
        else
        {
            return [hm![2], hm[3], hm[0], hm[1], hm[4]];
        }
    }

    /**
     * Does a Substring of shorttext exist within longtext such that the
     * Substring is at least half the length of longtext?
     * @param longtext Longer string.
     * @param shorttext Shorter string.
     * @param i Start index of quarter length Substring within longtext.
     * @return Five element string array, containing the prefix of longtext, the
     *     suffix of longtext, the prefix of shorttext, the suffix of shorttext
     *     and the common middle.  Or null if there was no match.
     */
    [Pure]
    private Rope<char>[] diff_halfMatchI(Rope<char> longtext, Rope<char> shorttext, long i)
    {
        // Start with a 1/4 length Substring at position i as a seed.
        var seed = longtext.Slice(i, longtext.Length / 4);
        long j = -1;
        var best_common = Rope<char>.Empty;
        var best_longtext_a = Rope<char>.Empty;
        var best_longtext_b = Rope<char>.Empty;
        var best_shorttext_a = Rope<char>.Empty;
        var best_shorttext_b = Rope<char>.Empty;
        while (j < shorttext.Length && (j = shorttext.IndexOf(seed, j + 1)) != -1)
        {
            int prefixLength = diff_commonPrefix(longtext.Slice(i), shorttext.Slice(j));
            int suffixLength = diff_commonSuffix(longtext.Slice(0, i), shorttext.Slice(0, j));
            if (best_common.Length < suffixLength + prefixLength)
            {
                best_common = shorttext.Slice(j - suffixLength, suffixLength).AddRange(shorttext.Slice(j, prefixLength));
                best_longtext_a = longtext.Slice(0, i - suffixLength);
                best_longtext_b = longtext.Slice(i + prefixLength);
                best_shorttext_a = shorttext.Slice(0, j - suffixLength);
                best_shorttext_b = shorttext.Slice(j + prefixLength);
            }
        }

        if (best_common.Length * 2 >= longtext.Length)
        {
            return new Rope<char>[]
            {
                best_longtext_a,
                best_longtext_b,
                best_shorttext_a,
                best_shorttext_b,
                best_common
            };
        }
        else
        {
            return null;
        }
    }

    [Obsolete("diff_cleanupSemantic_pure", true)]
    public void diff_cleanupSemantic(List<Diff> diffs)
    {
    }
    /**
     * Reduce the number of edits by eliminating semantically trivial
     * equalities.
     * @param diffs List of Diff objects.
     */
    [Pure]
    public Rope<Diff> diff_cleanupSemantic_pure(Rope<Diff> diffs)
    {
        bool changes = false;
        // Stack of indices where equalities are found.
        Stack<long> equalities = new Stack<long>();

        // Always equal to equalities[equalitiesLength-1][1]
        var lastEquality = Rope<char>.Empty;
        long pointer = 0;  // Index of current position.
                           // Number of characters that changed prior to the equality.
        long length_insertions1 = 0;
        long length_deletions1 = 0;
        // Number of characters that changed after the equality.
        long length_insertions2 = 0;
        long length_deletions2 = 0;
        while (pointer < diffs.Count)
        {
            if (diffs[pointer].Operation == Operation.EQUAL)
            {  // Equality found.
                equalities.Push(pointer);
                length_insertions1 = length_insertions2;
                length_deletions1 = length_deletions2;
                length_insertions2 = 0;
                length_deletions2 = 0;
                lastEquality = diffs[pointer].Text;
            }
            else
            {  // an insertion or deletion
                if (diffs[pointer].Operation == Operation.INSERT)
                {
                    length_insertions2 += diffs[pointer].Text.Length;
                }
                else
                {
                    length_deletions2 += diffs[pointer].Text.Length;
                }

                // Eliminate an equality that is smaller or equal to the edits on both
                // sides of it.
                if (!lastEquality.IsEmpty && (lastEquality.Length
                    <= Math.Max(length_insertions1, length_deletions1))
                    && (lastEquality.Length <= Math.Max(length_insertions2, length_deletions2)))
                {
                    // Duplicate record.
                    diffs = diffs.Insert(equalities.Peek(), new Diff(Operation.DELETE, lastEquality));

                    // Change second copy to insert.
                    diffs = diffs.SetItem(equalities.Peek() + 1, new Diff(Operation.INSERT, diffs[equalities.Peek() + 1].Text));

                    // Throw away the equality we just deleted.
                    equalities.Pop();
                    if (equalities.Count > 0)
                    {
                        equalities.Pop();
                    }

                    pointer = equalities.Count > 0 ? equalities.Peek() : -1;
                    length_insertions1 = 0;  // Reset the counters.
                    length_deletions1 = 0;
                    length_insertions2 = 0;
                    length_deletions2 = 0;
                    lastEquality = Rope<char>.Empty;
                    changes = true;
                }
            }

            pointer++;
        }

        // Normalize the diff.
        if (changes)
        {
            diffs = diff_cleanupMerge_pure(diffs);
        }

        diffs = diff_cleanupSemanticLossless_pure(diffs);

        // Find any overlaps between deletions and insertions.
        // e.g: <del>abcxxx</del><ins>xxxdef</ins>
        //   -> <del>abc</del>xxx<ins>def</ins>
        // e.g: <del>xxxabc</del><ins>defxxx</ins>
        //   -> <ins>def</ins>xxx<del>abc</del>
        // Only extract an overlap if it is as big as the edit ahead or behind it.
        pointer = 1;
        while (pointer < diffs.Count)
        {
            if (diffs[pointer - 1].Operation == Operation.DELETE && diffs[pointer].Operation == Operation.INSERT)
            {
                var deletion = diffs[pointer - 1].Text;
                var insertion = diffs[pointer].Text;
                int overlap_length1 = diff_commonOverlap(deletion, insertion);
                int overlap_length2 = diff_commonOverlap(insertion, deletion);
                if (overlap_length1 >= overlap_length2)
                {
                    if (overlap_length1 >= deletion.Length / 2.0 ||
                        overlap_length1 >= insertion.Length / 2.0)
                    {
                        // Overlap found.
                        // Insert an equality and trim the surrounding edits.
                        diffs = diffs.Insert(pointer, new Diff(Operation.EQUAL, insertion.Slice(0, overlap_length1)));
                        diffs = diffs.SetItem(pointer - 1, diffs[pointer - 1].WithText(deletion.Slice(0, deletion.Length - overlap_length1)));
                        diffs = diffs.SetItem(pointer + 1, diffs[pointer + 1].WithText(insertion.Slice(overlap_length1)));
                        pointer++;
                    }
                }
                else
                {
                    if (overlap_length2 >= deletion.Length / 2.0 || overlap_length2 >= insertion.Length / 2.0)
                    {
                        // Reverse overlap found.
                        // Insert an equality and swap and trim the surrounding edits.
                        diffs = diffs.Insert(pointer, new Diff(Operation.EQUAL, deletion.Slice(0, overlap_length2)));
                        diffs = diffs.SetItem(pointer - 1, new Diff(Operation.INSERT, insertion.Slice(0, insertion.Length - overlap_length2)));
                        diffs = diffs.SetItem(pointer + 1, new Diff(Operation.DELETE, deletion.Slice(overlap_length2)));
                        pointer++;
                    }
                }

                pointer++;
            }

            pointer++;
        }

        return diffs;
    }

    [Obsolete("Use pure", true)]
    public void diff_cleanupSemanticLossless(List<Diff> diffs)
    {
    }

    /**
     * Look for single edits surrounded on both sides by equalities
     * which can be shifted sideways to align the edit to a word boundary.
     * e.g: The c<ins>at c</ins>ame. -> The <ins>cat </ins>came.
     * @param diffs List of Diff objects.
     */
    [Pure]
    public Rope<Diff> diff_cleanupSemanticLossless_pure(Rope<Diff> inputDiffs)
    {
        var diffs = inputDiffs;
        int pointer = 1;

        // Intentionally ignore the first and last element (don't need checking).
        while (pointer < diffs.Count - 1)
        {
            if (diffs[pointer - 1].Operation == Operation.EQUAL && diffs[pointer + 1].Operation == Operation.EQUAL)
            {
                // This is a single edit surrounded by equalities.
                var equality1 = diffs[pointer - 1].Text;
                var edit = diffs[pointer].Text;
                var equality2 = diffs[pointer + 1].Text;

                // First, shift the edit as far left as possible.
                int commonOffset = this.diff_commonSuffix(equality1, edit);
                if (commonOffset > 0)
                {
                    var commonString = edit.Slice(edit.Length - commonOffset);
                    equality1 = equality1.Slice(0, equality1.Length - commonOffset);
                    edit = commonString.Concat(edit.Slice(0, edit.Length - commonOffset));
                    equality2 = commonString.Concat(equality2);
                }

                // Second, step character by character right,
                // looking for the best fit.
                var bestEquality1 = equality1;
                var bestEdit = edit;
                var bestEquality2 = equality2;
                int bestScore = diff_cleanupSemanticScore(equality1, edit) + diff_cleanupSemanticScore(edit, equality2);
                while (edit.Length != 0 && equality2.Length != 0 && edit[0] == equality2[0])
                {
                    equality1 = equality1.Concat(edit.Slice(0, 1));
                    edit = edit.Slice(1).Concat(equality2.Slice(0, 1));
                    equality2 = equality2.Slice(1);
                    int score = diff_cleanupSemanticScore(equality1, edit) + diff_cleanupSemanticScore(edit, equality2);
                    // The >= encourages trailing rather than leading whitespace on
                    // edits.
                    if (score >= bestScore)
                    {
                        bestScore = score;
                        bestEquality1 = equality1;
                        bestEdit = edit;
                        bestEquality2 = equality2;
                    }
                }

                if (!diffs[pointer - 1].Text.Equals(bestEquality1))
                {
                    // We have an improvement, save it back to the diff.
                    if (bestEquality1.Length != 0)
                    {
                        diffs = diffs.SetItem(pointer - 1, diffs[pointer - 1].WithText(bestEquality1));
                    }
                    else
                    {
                        diffs = diffs.RemoveAt(pointer - 1);
                        pointer--;
                    }

                    diffs = diffs.SetItem(pointer, new Diff(diffs[pointer].Operation, bestEdit));
                    if (bestEquality2.Length != 0)
                    {
                        diffs = diffs.SetItem(pointer + 1, diffs[pointer + 1].WithText(bestEquality2));
                    }
                    else
                    {
                        diffs = diffs.RemoveAt(pointer + 1);
                        pointer--;
                    }
                }
            }

            pointer++;
        }

        return diffs;
    }

    /**
     * Given two strings, compute a score representing whether the internal
     * boundary falls on logical boundaries.
     * Scores range from 6 (best) to 0 (worst).
     * @param one First string.
     * @param two Second string.
     * @return The score.
     */
    [Pure]
    private int diff_cleanupSemanticScore(Rope<char> one, Rope<char> two)
    {
        if (one.Length == 0 || two.Length == 0)
        {
            // Edges are the best.
            return 6;
        }

        // Each port of this function behaves slightly differently due to
        // subtle differences in each language's definition of things like
        // 'whitespace'.  Since this function's purpose is largely cosmetic,
        // the choice has been made to use each language's native features
        // rather than force total conformity.
        char char1 = one[one.Length - 1];
        char char2 = two[0];
        bool nonAlphaNumeric1 = !char.IsLetterOrDigit(char1);
        bool nonAlphaNumeric2 = !char.IsLetterOrDigit(char2);
        bool whitespace1 = nonAlphaNumeric1 && char.IsWhiteSpace(char1);
        bool whitespace2 = nonAlphaNumeric2 && char.IsWhiteSpace(char2);
        bool lineBreak1 = whitespace1 && char.IsControl(char1);
        bool lineBreak2 = whitespace2 && char.IsControl(char2);
        bool blankLine1 = lineBreak1 && BLANKLINEEND.IsMatch(one.ToString()); // TODO: Regex for Rope<char>
        bool blankLine2 = lineBreak2 && BLANKLINESTART.IsMatch(two.ToString());  // TODO: Regex for Rope<char>

        if (blankLine1 || blankLine2)
        {
            // Five points for blank lines.
            return 5;
        }
        else if (lineBreak1 || lineBreak2)
        {
            // Four points for line breaks.
            return 4;
        }
        else if (nonAlphaNumeric1 && !whitespace1 && whitespace2)
        {
            // Three points for end of sentences.
            return 3;
        }
        else if (whitespace1 || whitespace2)
        {
            // Two points for whitespace.
            return 2;
        }
        else if (nonAlphaNumeric1 || nonAlphaNumeric2)
        {
            // One point for non-alphanumeric.
            return 1;
        }

        return 0;
    }

    // Define some regex patterns for matching boundaries.
    private Regex BLANKLINEEND = new Regex("\\n\\r?\\n\\Z");
    private Regex BLANKLINESTART = new Regex("\\A\\r?\\n\\r?\\n");

    [Obsolete("Use pure", true)]
    public void diff_cleanupEfficiency(List<Diff> diffs)
    {
    }
 
    /**
    * Reduce the number of edits by eliminating operationally trivial
    * equalities.
    * @param diffs List of Diff objects.
    */
    [Pure]
    public Rope<Diff> diff_cleanupEfficiency_pure(Rope<Diff> diffs)
    {
        bool changes = false;
        
        // Stack of indices where equalities are found.
        var equalities = new Stack<long>();
        
        // Always equal to equalities[equalitiesLength-1][1]
        var lastEquality = Rope<char>.Empty;
        
        // Index of current position.
        long pointer = 0;  
        
        // Is there an insertion operation before the last equality.
        bool pre_ins = false;
        
        // Is there a deletion operation before the last equality.
        bool pre_del = false;
        
        // Is there an insertion operation after the last equality.
        bool post_ins = false;
        
        // Is there a deletion operation after the last equality.
        bool post_del = false;
        while (pointer < diffs.Count)
        {
            if (diffs[pointer].Operation == Operation.EQUAL)
            {
                // Equality found.
                if (diffs[pointer].Text.Length < this.Diff_EditCost && (post_ins || post_del))
                {
                    // Candidate found.
                    equalities.Push(pointer);
                    pre_ins = post_ins;
                    pre_del = post_del;
                    lastEquality = diffs[pointer].Text;
                }
                else
                {
                    // Not a candidate, and can never become one.
                    equalities.Clear();
                    lastEquality = ReadOnlyMemory<char>.Empty;
                }
                post_ins = post_del = false;
            }
            else
            {  // An insertion or deletion.
                if (diffs[pointer].Operation == Operation.DELETE)
                {
                    post_del = true;
                }
                else
                {
                    post_ins = true;
                }

                
				 // Five types to be split:
				 // <ins>A</ins><del>B</del>XY<ins>C</ins><del>D</del>
				 // <ins>A</ins>X<ins>C</ins><del>D</del>
				 // <ins>A</ins><del>B</del>X<ins>C</ins>
				 // <ins>A</del>X<ins>C</ins><del>D</del>
				 // <ins>A</ins><del>B</del>X<del>C</del>				 
                if ((lastEquality.Length != 0)
                    && ((pre_ins && pre_del && post_ins && post_del)
                    || ((lastEquality.Length < this.Diff_EditCost / 2)
                    && ((pre_ins ? 1 : 0) + (pre_del ? 1 : 0) + (post_ins ? 1 : 0)
                    + (post_del ? 1 : 0)) == 3)))
                {
                    // Duplicate record.
                    diffs = diffs.Insert(equalities.Peek(), new Diff(Operation.DELETE, lastEquality));
                    // Change second copy to insert.
                    diffs = diffs.SetItem(equalities.Peek() + 1, diffs[equalities.Peek() + 1].WithOperation(Operation.INSERT));
                    equalities.Pop();  // Throw away the equality we just deleted.
                    lastEquality = ReadOnlyMemory<char>.Empty;
                    if (pre_ins && pre_del)
                    {
                        // No changes made which could affect previous entry, keep going.
                        post_ins = post_del = true;
                        equalities.Clear();
                    }
                    else
                    {
                        if (equalities.Count > 0)
                        {
                            equalities.Pop();
                        }

                        pointer = equalities.Count > 0 ? equalities.Peek() : -1;
                        post_ins = post_del = false;
                    }
                    changes = true;
                }
            }
            pointer++;
        }

        if (changes)
        {
            diffs = diff_cleanupMerge_pure(diffs);
        }

        return diffs;
    }

    [Obsolete("Use pure", true)]
    public void diff_cleanupMerge(List<Diff> diffs)
    {

    }

    /**
     * Reorder and merge like edit sections.  Merge equalities.
     * Any edit section can move as long as it doesn't cross an equality.
     * @param diffs List of Diff objects.
     */
    [Pure]
    public Rope<Diff> diff_cleanupMerge_pure(IEnumerable<Diff> inputDiffs)
    {
        var diffs = inputDiffs.ToRope();

        // Add a dummy entry at the end.
        diffs = diffs.Add(new Diff(Operation.EQUAL, Rope<char>.Empty));
        int pointer = 0;
        int count_delete = 0;
        int count_insert = 0;
        var text_delete = Rope<char>.Empty;
        var text_insert = Rope<char>.Empty;
        int commonlength;
        while (pointer < diffs.Count)
        {
            switch (diffs[pointer].Operation)
            {
                case Operation.INSERT:
                    count_insert++;
                    text_insert = text_insert.AddRange(diffs[pointer].Text);
                    pointer++;
                    break;
                case Operation.DELETE:
                    count_delete++;
                    text_delete = text_delete.AddRange(diffs[pointer].Text);
                    pointer++;
                    break;
                case Operation.EQUAL:
                    // Upon reaching an equality, check for prior redundancies.
                    if (count_delete + count_insert > 1)
                    {
                        if (count_delete != 0 && count_insert != 0)
                        {
                            // Factor out any common prefixes.
                            commonlength = this.diff_commonPrefix(text_insert, text_delete);
                            if (commonlength != 0)
                            {
                                var t = pointer - count_delete - count_insert - 1;
                                if (t >= 0 && diffs[t].Operation == Operation.EQUAL)
                                {
                                    diffs = diffs.SetItem(t, diffs[t].Append(text_insert.Slice(0, commonlength)));
                                }
                                else
                                {
                                    diffs = diffs.Insert(0, new Diff(Operation.EQUAL, text_insert.Slice(0, commonlength)));
                                    pointer++;
                                }
                                text_insert = text_insert.Slice(commonlength);
                                text_delete = text_delete.Slice(commonlength);
                            }
                            // Factor out any common suffixies.
                            commonlength = this.diff_commonSuffix(text_insert, text_delete);
                            if (commonlength != 0)
                            {
                                diffs = diffs.SetItem(pointer, diffs[pointer].Prepend(text_insert.Slice(text_insert.Length - commonlength)));
                                text_insert = text_insert.Slice(0, text_insert.Length - commonlength);
                                text_delete = text_delete.Slice(0, text_delete.Length - commonlength);
                            }
                        }

                        // Delete the offending records and add the merged ones.
                        pointer -= count_delete + count_insert;
                        (_, diffs) = diffs.Splice(pointer, count_delete + count_insert);
                        if (text_delete.Length != 0)
                        {
                            (_, diffs) = diffs.Splice(pointer, 0, new Diff(Operation.DELETE, text_delete));
                            pointer++;
                        }
                        if (text_insert.Length != 0)
                        {
                            (_, diffs) = diffs.Splice(pointer, 0, new Diff(Operation.INSERT, text_insert));
                            pointer++;
                        }
                        pointer++;
                    }
                    else if (pointer != 0 && diffs[pointer - 1].Operation == Operation.EQUAL)
                    {
                        // Merge this equality with the previous one.
                        diffs = diffs.SetItem(pointer - 1, diffs[pointer - 1].Append(diffs[pointer].Text));
                        diffs = diffs.RemoveAt(pointer);
                    }
                    else
                    {
                        pointer++;
                    }

                    count_insert = 0;
                    count_delete = 0;
                    text_delete = Rope<char>.Empty;
                    text_insert = Rope<char>.Empty;
                    break;
            }
        }

        if (diffs[diffs.Count - 1].Text.Length == 0)
        {
            diffs = diffs.RemoveAt(diffs.Count - 1);  // Remove the dummy entry at the end.
        }

        // Second pass: look for single edits surrounded on both sides by
        // equalities which can be shifted sideways to eliminate an equality.
        // e.g: A<ins>BA</ins>C -> <ins>AB</ins>AC
        bool changes = false;
        pointer = 1;
        // Intentionally ignore the first and last element (don't need checking).
        while (pointer < (diffs.Count - 1))
        {
            if (diffs[pointer - 1].Operation == Operation.EQUAL && diffs[pointer + 1].Operation == Operation.EQUAL)
            {
                // This is a single edit surrounded by equalities.
                if (diffs[pointer].Text.EndsWith(diffs[pointer - 1].Text))
                {
                    // Shift the edit over the previous equality.
                    diffs = diffs.SetItem(pointer, diffs[pointer].WithText(diffs[pointer - 1].Text.Concat(diffs[pointer].Text.Slice(0, diffs[pointer].Text.Length - diffs[pointer - 1].Text.Length))));
                    diffs = diffs.SetItem(pointer + 1, diffs[pointer + 1].WithText(diffs[pointer - 1].Text.Concat(diffs[pointer + 1].Text)));
                    (_, diffs) = diffs.Splice(pointer - 1, 1);
                    changes = true;
                }
                else if (diffs[pointer].Text.StartsWith(diffs[pointer + 1].Text))
                {
                    // Shift the edit over the next equality.
                    diffs = diffs.SetItem(pointer - 1, diffs[pointer - 1].Append(diffs[pointer + 1].Text));
                    diffs = diffs.SetItem(pointer, diffs[pointer].WithText(diffs[pointer].Text.Slice(diffs[pointer + 1].Text.Length).Concat(diffs[pointer + 1].Text)));
                    (_, diffs) = diffs.Splice(pointer + 1, 1);
                    changes = true;
                }
            }

            pointer++;
        }

        // If shifts were made, the diff needs reordering and another shift sweep.
        if (changes)
        {
            diffs = this.diff_cleanupMerge_pure(diffs);
        }

        return diffs;
    }

    /**
     * loc is a location in text1, compute and return the equivalent location in
     * text2.
     * e.g. "The cat" vs "The big cat", 1->1, 5->8
     * @param diffs List of Diff objects.
     * @param loc Location within text1.
     * @return Location within text2.
     */
    [Pure]
    public long diff_xIndex(IEnumerable<Diff> diffs, long loc)
    {
        long chars1 = 0;
        long chars2 = 0;
        long last_chars1 = 0;
        long last_chars2 = 0;
        Diff lastDiff = default;
        foreach (Diff aDiff in diffs)
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
            if (chars1 > loc)
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
        return last_chars2 + (loc - last_chars1);
    }

    /**
     * Convert a Diff list into a pretty HTML report.
     * @param diffs List of Diff objects.
     * @return HTML representation.
     */
    [Pure]
    public string diff_prettyHtml(IEnumerable<Diff> diffs)
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

        return html.ToString();
    }

    /**
     * Compute and return the source text (all equalities and deletions).
     * @param diffs List of Diff objects.
     * @return Source text.
     */
    [Pure]
    public Rope<char> diff_text1(IReadOnlyList<Diff> diffs)
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

    /**
     * Compute and return the destination text (all equalities and insertions).
     * @param diffs List of Diff objects.
     * @return Destination text.
     */
    [Pure]
    public Rope<char> diff_text2(IReadOnlyList<Diff> diffs)
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

    /**
     * Compute the Levenshtein distance; the number of inserted, deleted or
     * substituted characters.
     * @param diffs List of Diff objects.
     * @return Number of changes.
     */
    [Pure]
    public long diff_levenshtein(Rope<Diff> diffs)
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

    /**
     * Crush the diff into an encoded string which describes the operations
     * required to transform text1 into text2.
     * E.g. =3\t-2\t+ing  -> Keep 3 chars, delete 2 chars, insert 'ing'.
     * Operations are tab-separated.  Inserted text is escaped using %xx
     * notation.
     * @param diffs Array of Diff objects.
     * @return Delta text.
     */
    [Pure]
    public Rope<char> diff_toDelta(Rope<Diff> diffs)
    {
        var text = Rope<char>.Empty;
        foreach (Diff aDiff in diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.INSERT:
                    text = text.Append("+").AddRange(encodeURI(aDiff.Text)).Append("\t");
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

    /**
     * Given the original text1, and an encoded string which describes the
     * operations required to transform text1 into text2, compute the full diff.
     * @param text1 Source string for the diff.
     * @param delta Delta text.
     * @return Array of Diff objects or null if invalid.
     * @throws ArgumentException If invalid input.
     */
    [Pure]
    public Rope<Diff> diff_fromDelta(Rope<char> text1, Rope<char> delta)
    {
        var diffs = Rope<Diff>.Empty;
        int pointer = 0;  // Cursor in text1
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
                    // decode would change all "+" to " "
                    param = param.Replace("+", "%2b");
                    param = HttpUtility.UrlDecode(param.ToString()).ToRope();

                    //} catch (UnsupportedEncodingException e) {
                    //  // Not likely on modern system.
                    //  throw new Error("This system does not support UTF-8.", e);
                    //} catch (IllegalArgumentException e) {
                    //  // Malformed URI sequence.
                    //  throw new IllegalArgumentException(
                    //      "Illegal escape in diff_fromDelta: " + param, e);
                    //}
                    diffs = diffs.Add(new Diff(Operation.INSERT, param));
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
                        throw new ArgumentException("Invalid number in diff_fromDelta: " + param, e);
                    }
                    if (n < 0)
                    {
                        throw new ArgumentException(
                            "Negative number in diff_fromDelta: " + param);
                    }

                    Rope<char> text;
                    try
                    {
                        text = text1.Slice(pointer, n);
                        pointer += n;
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        throw new ArgumentException("Delta length (" + pointer
                            + ") larger than source text length (" + text1.Length
                            + ").", e);
                    }

                    if (token[0] == '=')
                    {
                        diffs = diffs.Add(new Diff(Operation.EQUAL, text));
                    }
                    else
                    {
                        diffs = diffs.Add(new Diff(Operation.DELETE, text));
                    }

                    break;
                default:
                    // Anything else is an error.
                    throw new ArgumentException(
                        "Invalid diff operation in diff_fromDelta: " + token[0]);
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
    public int match_main(string text, string pattern, int loc) => (int)match_main(text.ToRope(), pattern.ToRope(), loc);

    /**
     * Locate the best instance of 'pattern' in 'text' near 'loc'.
     * Returns -1 if no match found.
     * @param text The text to search.
     * @param pattern The pattern to search for.
     * @param loc The location to search around.
     * @return Best match index or -1.
     */
    [Pure]
    public long match_main(Rope<char> text, Rope<char> pattern, long loc)
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
            return match_bitap(text, pattern, loc);
        }
    }

    /**
     * Locate the best instance of 'pattern' in 'text' near 'loc' using the
     * Bitap algorithm.  Returns -1 if no match found.
     * @param text The text to search.
     * @param pattern The pattern to search for.
     * @param loc The location to search around.
     * @return Best match index or -1.
     */
    [Pure]
    protected int match_bitap(string text,string pattern, int loc) => (int)match_bitap(text.ToRope(), pattern.ToRope(), loc);

    [Pure]
    protected long match_bitap(Rope<char> text, Rope<char> pattern, long loc)
    {
        // assert (Match_MaxBits == 0 || pattern.Length <= Match_MaxBits)
        //    : "Pattern too long for this application.";

        // Initialise the alphabet.
        Dictionary<char, long> s = match_alphabet(pattern);

        // Highest score beyond which we give up.
        double score_threshold = Match_Threshold;
        // Is there a nearby exact match? (speedup)
        var best_loc = text.IndexOf(pattern, loc);
        if (best_loc != -1)
        {
            score_threshold = Math.Min(match_bitapScore(0, best_loc, loc, pattern), score_threshold);
            // What about in the other direction? (speedup)
            best_loc = text.LastIndexOf(pattern, Math.Min(loc + pattern.Length, text.Length));
            if (best_loc != -1)
            {
                score_threshold = Math.Min(match_bitapScore(0, best_loc, loc, pattern), score_threshold);
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
                if (match_bitapScore(d, loc + bin_mid, loc, pattern)
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
                    double score = match_bitapScore(d, j - 1, loc, pattern);
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
            if (match_bitapScore(d + 1, loc, loc, pattern) > score_threshold)
            {
                // No hope for a (better) match at greater error levels.
                break;
            }
            last_rd = rd;
        }
        return best_loc;
    }

    /**
     * Compute and return the score for a match with e errors and x location.
     * @param e Number of errors in match.
     * @param x Location of match.
     * @param loc Expected location of match.
     * @param pattern Pattern being sought.
     * @return Overall score for match (0.0 = good, 1.0 = bad).
     */
    [Pure]
    private double match_bitapScore(long e, long x, long loc, Rope<char> pattern)
    {
        float accuracy = (float)e / pattern.Length;
        var proximity = Math.Abs(loc - x);
        if (Match_Distance == 0)
        {
            // Dodge divide by zero error.
            return proximity == 0 ? accuracy : 1.0;
        }
        return accuracy + (proximity / (float)Match_Distance);
    }

    [Pure]
    protected Dictionary<char, long> match_alphabet(string pattern) => match_alphabet(pattern.ToRope());

    /**
     * Initialise the alphabet for the Bitap algorithm.
     * @param pattern The text to encode.
     * @return Hash of character locations.
     */
    [Pure]
    protected Dictionary<char, long> match_alphabet(Rope<char> pattern)
    {
        Dictionary<char, long> s = new Dictionary<char, long>();        
        foreach (char c in pattern)
        {
            if (!s.ContainsKey(c))
            {
                s.Add(c, 0);
            }
        }

        long i = 0;
        foreach (char c in pattern)
        {
            long value = s[c] | (1L << (int)(pattern.Length - i - 1));
            s[c] = value;
            i++;
        }

        return s;
    }


    //  PATCH FUNCTIONS


    /**
     * Increase the context until it is unique,
     * but don't let the pattern expand beyond Match_MaxBits.
     * @param patch The patch to grow.
     * @param text Source text.
     */
    [Obsolete("Use pure version", true)]
    protected void patch_addContext(Patch patch, string text)
    {
        //      if (text.Length == 0) {
        //        return;
        //      }
        //      string pattern = text.Substring(patch.start2, patch.length1);
        //      int padding = 0;
        //
        //      // Look for the first and last matches of pattern in text.  If two
        //      // different matches are found, increase the pattern length.
        //      while (text.IndexOf(pattern, StringComparison.Ordinal)
        //          != text.LastIndexOf(pattern, StringComparison.Ordinal)
        //          && pattern.Length < Match_MaxBits - Patch_Margin - Patch_Margin) {
        //        padding += Patch_Margin;
        //        pattern = text.JavaSubstring(Math.Max(0, patch.start2 - padding),
        //            Math.Min(text.Length, patch.start2 + patch.length1 + padding));
        //      }
        //      // Add one chunk for good luck.
        //      padding += Patch_Margin;
        //
        //      // Add the prefix.
        //      string prefix = text.JavaSubstring(Math.Max(0, patch.start2 - padding), patch.start2);
        //      if (prefix.Length != 0) {
        //        patch.diffs = patch.diffs.Insert(0, new Diff(Operation.EQUAL, prefix));
        //      }
        //      // Add the suffix.
        //      string suffix = text.JavaSubstring(patch.start2 + patch.length1,
        //          Math.Min(text.Length, patch.start2 + patch.length1 + padding));
        //      if (suffix.Length != 0) {
        //        patch.diffs = patch.diffs.Add(new Diff(Operation.EQUAL, suffix));
        //      }
        //
        //      // Roll back the start points.
        //      patch.start1 -= prefix.Length;
        //      patch.start2 -= prefix.Length;
        //	  
        //      // Extend the lengths.
        //      patch.length1 += prefix.Length + suffix.Length;
        //      patch.length2 += prefix.Length + suffix.Length;
    }

    [Pure]
    protected Patch patch_addContext_pure(Patch patch, Rope<char> text)
    {
        if (text.Length == 0)
        {
            return patch;
        }

        var pattern = text.Slice(patch.Start2, patch.Length1);
        int padding = 0;

        // Look for the first and last matches of pattern in text.  If two
        // different matches are found, increase the pattern length.
        while (text.IndexOf(pattern) != text.LastIndexOf(pattern) && pattern.Length < Match_MaxBits - Patch_Margin - Patch_Margin)
        {
            padding += Patch_Margin;
            pattern = text.JavaSubstring(Math.Max(0, patch.Start2 - padding), Math.Min(text.Length, patch.Start2 + patch.Length1 + padding));
        }
        // Add one chunk for good luck.
        padding += Patch_Margin;

        // Add the prefix.
        var prefix = text.JavaSubstring(Math.Max(0, patch.Start2 - padding), patch.Start2);
        if (prefix.Length != 0)
        {
            patch = patch with { Diffs = patch.Diffs.Insert(0, new Diff(Operation.EQUAL, prefix)) };
        }

        // Add the suffix.
        var suffix = text.JavaSubstring(patch.Start2 + patch.Length1, Math.Min(text.Length, patch.Start2 + patch.Length1 + padding));
        if (suffix.Length != 0)
        {
            patch = patch with { Diffs = patch.Diffs.Add(new Diff(Operation.EQUAL, suffix)) };
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

    [Pure]
    public IEnumerable<Patch> patch_make(string text1, string text2) => patch_make(text1.ToRope(), text2.ToRope());

    /**
     * Compute a list of patches to turn text1 into text2.
     * A set of diffs will be computed.
     * @param text1 Old text.
     * @param text2 New text.
     * @return List of Patch objects.
     */
    [Pure]
    public IEnumerable<Patch> patch_make(Rope<char> text1, Rope<char> text2)
    {
        // Check for null inputs not needed since null can't be passed in C#.
        // No diffs provided, compute our own.
        var diffs = diff_main(text1, text2, true);
        if (diffs.Count > 2)
        {
            diffs = diff_cleanupSemantic_pure(diffs);
            diffs = diff_cleanupEfficiency_pure(diffs);
        }

        return patch_make(text1, diffs);
    }

    /**
     * Compute a list of patches to turn text1 into text2.
     * text1 will be derived from the provided diffs.
     * @param diffs Array of Diff objects for text1 to text2.
     * @return List of Patch objects.
     */
    [Pure]
    public IEnumerable<Patch> patch_make(Rope<Diff> diffs)
    {
        // No origin string provided, compute our own.
        var text1 = diff_text1(diffs);
        return patch_make(text1, diffs);
    }

    /**
     * Compute a list of patches to turn text1 into text2.
     * text2 is ignored, diffs are the delta between text1 and text2.
     * @param text1 Old text
     * @param text2 Ignored.
     * @param diffs Array of Diff objects for text1 to text2.
     * @return List of Patch objects.
     * @deprecated Prefer patch_make(string text1, List<Diff> diffs).
     */
    [Obsolete("Prefer patch_make(Rope<char text1, List<Diff> diffs).")]
    public IEnumerable<Patch> patch_make(Rope<char> text1, Rope<char> text2, Rope<Diff> diffs) => patch_make(text1, diffs);

    /**
     * Compute a list of patches to turn text1 into text2.
     * text2 is not provided, diffs are the delta between text1 and text2.
     * @param text1 Old text.
     * @param diffs Array of Diff objects for text1 to text2.
     * @return List of Patch objects.
     */
    [Pure]
    public IEnumerable<Patch> patch_make(Rope<char> text1, Rope<Diff> diffs)
    {
        // Check for null inputs not needed since null can't be passed in C#.
        if (diffs.Count == 0)
        {
            yield break;
        }

        Patch patch = new Patch();
        long char_count1 = 0;  // Number of characters into the text1 string.
        long char_count2 = 0;  // Number of characters into the text2 string.
                              // Start with text1 (prepatch_text) and apply the diffs until we arrive at
                              // text2 (postpatch_text). We recreate the patches one by one to determine
                              // context info.
        var prepatch_text = text1;
        var postpatch_text = text1;
        foreach (Diff aDiff in diffs)
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
                    if (aDiff.Text.Length <= 2 * Patch_Margin && patch.Diffs.Count() != 0 && aDiff != diffs.Last())
                    {
                        // Small equality inside a patch.
                        patch = patch with
                        {
                            Diffs = patch.Diffs.Add(aDiff),
                            Length1 = patch.Length1 + aDiff.Text.Length,
                            Length2 = patch.Length2 + aDiff.Text.Length
                        };
                    }

                    if (aDiff.Text.Length >= 2 * Patch_Margin)
                    {
                        // Time for a new patch.
                        if (patch.Diffs.Count != 0)
                        {
                            patch = patch_addContext_pure(patch, prepatch_text);
                            yield return patch;

                            patch = new Patch();
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
            patch = patch_addContext_pure(patch, prepatch_text);
            yield return patch;
        }
    }

    /**
		 * Merge a set of patches onto the text.  Return a patched text, as well
		 * as an array of true/false values indicating which patches were applied.
		 * @param patches Array of Patch objects
		 * @param text Old text.
		 * @return Two element Object array, containing the new text and an array of
		 *      bool values.
		 */
    [Pure]
    public (string Text, bool[] Applied) patch_apply(IEnumerable<Patch> patches, string text)
    {
        var (result, applied) = patch_apply(patches.ToRope(), text.ToRope());
        return (result.ToString(), applied);
    }

    /**
     * Merge a set of patches onto the text.  Return a patched text, as well
     * as an array of true/false values indicating which patches were applied.
     * @param patches Array of Patch objects
     * @param text Old text.
     * @return Two element Object array, containing the new text and an array of
     *      bool values.
     */
    [Pure]
    public (Rope<char> Text, bool[] Applied) patch_apply(Rope<Patch> patches, Rope<char> text)
    {
        if (patches.Count == 0)
        {
            return (text, Array.Empty<bool>());
        }

        (var nullPadding, patches) = this.patch_addPadding_pure(patches);
        text = nullPadding + text + nullPadding;
        patches = this.patch_splitMax_pure(patches);

        long x = 0;
        // delta keeps track of the offset between the expected and actual
        // location of the previous patch.  If there are patches expected at
        // positions 10 and 20, but the first patch was found at 12, delta is 2
        // and the second patch has an effective expected position of 22.
        long delta = 0;
        bool[] results = new bool[patches.Count];
        foreach (Patch aPatch in patches)
        {
            var expected_loc = aPatch.Start2 + delta;
            var text1 = diff_text1(aPatch.Diffs);
            long start_loc;
            long end_loc = -1;
            if (text1.Length > this.Match_MaxBits)
            {
                // patch_splitMax will only provide an oversized pattern
                // in the case of a monster delete.
                start_loc = match_main(text, text1.Slice(0, this.Match_MaxBits), expected_loc);
                if (start_loc != -1)
                {
                    end_loc = match_main(text, text1.Slice(text1.Length - this.Match_MaxBits),
                        expected_loc + text1.Length - this.Match_MaxBits);
                    if (end_loc == -1 || start_loc >= end_loc)
                    {
                        // Can't find valid trailing context.  Drop this patch.
                        start_loc = -1;
                    }
                }
            }
            else
            {
                start_loc = this.match_main(text, text1, expected_loc);
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
                    text2 = text.JavaSubstring(start_loc, Math.Min(end_loc + this.Match_MaxBits, text.Length));
                }
                if (text1 == text2)
                {
                    // Perfect match, just shove the Replacement text in.
                    text = text.Slice(0, start_loc) + diff_text2(aPatch.Diffs) + text.Slice(start_loc + text1.Length);
                }
                else
                {
                    // Imperfect match.  Run a diff to get a framework of equivalent
                    // indices.
                    var diffs = diff_main(text1, text2, false);
                    if (text1.Length > this.Match_MaxBits && this.diff_levenshtein(diffs) / (float)text1.Length > this.Patch_DeleteThreshold)
                    {
                        // The end points match, but the content is unacceptably bad.
                        results[x] = false;
                    }
                    else
                    {
                        diffs = diff_cleanupSemanticLossless_pure(diffs);
                        long index1 = 0;
                        foreach (Diff aDiff in aPatch.Diffs)
                        {
                            if (aDiff.Operation != Operation.EQUAL)
                            {
                                var index2 = diff_xIndex(diffs, index1);
                                if (aDiff.Operation == Operation.INSERT)
                                {
                                    // Insertion
                                    text = text.InsertRange(start_loc + index2, aDiff.Text);
                                }
                                else if (aDiff.Operation == Operation.DELETE)
                                {
                                    // Deletion
                                    text = text.RemoveRange(start_loc + index2, diff_xIndex(diffs, index1 + aDiff.Text.Length) - index2);
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

    /**
     * Add some padding on text start and end so that edges can match something.
     * Intended to be called only from within patch_apply.
     * @param patches Array of Patch objects.
     * @return The padding string added to each side.
     */
    [Obsolete("use patch_addPadding_pure", true)]
    protected string patch_addPadding(List<Patch> patches)
    {
        throw new NotImplementedException("use patch_addPadding_pure");
        //      short paddingLength = this.Patch_Margin;
        //      string nullPadding = string.Empty;
        //      for (short x = 1; x <= paddingLength; x++) {
        //        nullPadding += (char)x;
        //      }
        //
        //      // Bump all the patches forward.
        //      foreach (Patch aPatch in patches)
        //	  {
        //        aPatch.start1 += paddingLength;
        //        aPatch.start2 += paddingLength;
        //      }
        //
        //      // Add some padding on start of first diff.
        //      Patch patch = patches[0];
        //      var diffs = patch.diffs;
        //      if (diffs.Count == 0 || diffs[0].Operation != Operation.EQUAL) {
        //        // Add nullPadding equality.
        //        diffs = diffs.Insert(0, new Diff(Operation.EQUAL, nullPadding));
        //        patch.start1 -= paddingLength;  // Should be 0.
        //        patch.start2 -= paddingLength;  // Should be 0.
        //        patch.length1 += paddingLength;
        //			patch.length2 += paddingLength;
        //		}
        //		else if (paddingLength > diffs[0].Text.Length)
        //		{
        //			// Grow first equality.
        //			Diff firstDiff = diffs[0];
        //			int extraLength = paddingLength - firstDiff.Text.Length;
        //			diffs = diffs.SetItem(0, firstDiff.Prepend(nullPadding.Substring(firstDiff.Text.Length)));
        //			patch.start1 -= extraLength;
        //			patch.start2 -= extraLength;
        //			patch.length1 += extraLength;
        //			patch.length2 += extraLength;
        //		}
        //
        //		patch.diffs = diffs;
        //		
        //		// Add some padding on end of last diff.
        //      patch = patches[^1];
        //      diffs = patch.diffs;
        //      if (diffs.Count == 0 || diffs.Last().Operation != Operation.EQUAL) {
        //        // Add nullPadding equality.
        //        diffs = diffs.Add(new Diff(Operation.EQUAL, nullPadding));
        //        patch.length1 += paddingLength;
        //			patch.length2 += paddingLength;
        //		}
        //		else if (paddingLength > diffs[^1].Text.Length)
        //		{
        //			// Grow last equality.
        //			Diff lastDiff = diffs[^1];
        //			int extraLength = paddingLength - lastDiff.Text.Length;
        //			diffs = diffs.SetItem(diffs.Count - 1, lastDiff.Append(nullPadding.Substring(0, extraLength)));
        //			patch.length1 += extraLength;
        //			patch.length2 += extraLength;
        //		}
        //	  
        //	  patch.diffs = diffs;
        //
        //      return nullPadding;
    }

    /**
	   * Add some padding on text start and end so that edges can match something.
	   * Intended to be called only from within patch_apply.
	   * @param patches Array of Patch objects.
	   * @return The padding string added to each side.
	   */
    [Pure]
    protected (Rope<char> NullPadding, Rope<Patch> BumpedPatches) patch_addPadding_pure(IEnumerable<Patch> patches)
    {
        short paddingLength = this.Patch_Margin;
        var nullPadding = Rope<char>.Empty;
        for (short x = 1; x <= paddingLength; x++)
        {
            nullPadding += (char)x;
        }

        // Bump all the patches forward.
        var bumpedPatches = patches.Select(p => p with { Start1 = p.Start1 + paddingLength, Start2 = p.Start2 + paddingLength }).ToRope();

        // Add some padding on start of first diff.
        Patch patch = bumpedPatches[0];
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
                Diffs = diffs.Insert(0, new Diff(Operation.EQUAL, nullPadding))
            };
        }
        else if (paddingLength > diffs[0].Text.Length)
        {
            // Grow first equality.
            Diff firstDiff = diffs[0];
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
                Diffs = diffs.Add(new Diff(Operation.EQUAL, nullPadding)),
            };
        }
        else if (paddingLength > diffs[^1].Text.Length)
        {
            // Grow last equality.
            Diff lastDiff = diffs[^1];
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

    /**
     * Look through the patches and break up any which are longer than the
     * maximum limit of the match algorithm.
     * Intended to be called only from within patch_apply.
     * @param patches List of Patch objects.
     */
    [Obsolete("use patch_splitMax_pure")]
    public void patch_splitMax(List<Patch> patches)
    {
        //      short patch_size = this.Match_MaxBits;
        //      for (int x = 0; x < patches.Count; x++) {
        //        if (patches[x].length1 <= patch_size) {
        //          continue;
        //        }
        //		
        //        Patch bigpatch = patches[x];
        //		
        //        // Remove the big old patch.
        //        patches.Splice(x--, 1);
        //        int start1 = bigpatch.start1;
        //        int start2 = bigpatch.start2;
        //        string precontext = string.Empty;
        //        while (bigpatch.diffs.Count != 0) {
        //          // Create one of several smaller patches.
        //          Patch patch = new Patch();
        //          bool empty = true;
        //          patch.start1 = start1 - precontext.Length;
        //          patch.start2 = start2 - precontext.Length;
        //          if (precontext.Length != 0) {
        //            patch.length1 = patch.length2 = precontext.Length;
        //            patch.diffs = patch.diffs.Add(new Diff(Operation.EQUAL, precontext));
        //          }
        //          while (bigpatch.diffs.Count != 0
        //              && patch.length1 < patch_size - this.Patch_Margin) {
        //            Operation diff_type = bigpatch.diffs[0].Operation;
        //            string diff_text = bigpatch.diffs[0].Text;
        //            if (diff_type == Operation.INSERT) {
        //              // Insertions are harmless.
        //              patch.length2 += diff_text.Length;
        //              start2 += diff_text.Length;
        //              patch.diffs = patch.diffs.Add(bigpatch.diffs.First());
        //              bigpatch.diffs = bigpatch.diffs.RemoveAt(0);
        //              empty = false;
        //            } else if (diff_type == Operation.DELETE && patch.diffs.Count == 1
        //                && patch.diffs.First().Operation == Operation.EQUAL
        //                && diff_text.Length > 2 * patch_size) {
        //              // This is a large deletion.  Let it pass in one chunk.
        //              patch.length1 += diff_text.Length;
        //              start1 += diff_text.Length;
        //              empty = false;
        //              patch.diffs = patch.diffs.Add(new Diff(diff_type, diff_text));
        //              bigpatch.diffs = bigpatch.diffs.RemoveAt(0);
        //            } else {
        //              // Deletion or equality.  Only take as much as we can stomach.
        //              diff_text = diff_text.Substring(0, Math.Min(diff_text.Length,
        //                  patch_size - patch.length1 - Patch_Margin));
        //              patch.length1 += diff_text.Length;
        //              start1 += diff_text.Length;
        //              if (diff_type == Operation.EQUAL) {
        //                patch.length2 += diff_text.Length;
        //                start2 += diff_text.Length;
        //              } else {
        //                empty = false;
        //              }
        //              patch.diffs = patch.diffs.Add(new Diff(diff_type, diff_text));
        //              if (diff_text == bigpatch.diffs[0].Text) {
        //                bigpatch.diffs = bigpatch.diffs.RemoveAt(0);
        //              } else {
        //                bigpatch.diffs = bigpatch.diffs.SetItem(0, bigpatch.diffs[0].WithText(bigpatch.diffs[0].Text.Substring(diff_text.Length)));
        //              }
        //            }
        //          }
        //          // Compute the head context for the next patch.
        //          precontext = this.diff_text2(patch.diffs);
        //          precontext = precontext.Substring(Math.Max(0, precontext.Length - this.Patch_Margin));
        //
        //          string postcontext = null;
        //          // Append the end context for this patch.
        //          if (diff_text1(bigpatch.diffs).Length > Patch_Margin) {
        //            postcontext = diff_text1(bigpatch.diffs).Substring(0, Patch_Margin);
        //          } else {
        //            postcontext = diff_text1(bigpatch.diffs);
        //          }
        //
        //          if (postcontext.Length != 0) {
        //            patch.length1 += postcontext.Length;
        //            patch.length2 += postcontext.Length;
        //            if (patch.diffs.Count != 0 && patch.diffs[patch.diffs.Count - 1].Operation == Operation.EQUAL) {
        //              patch.diffs = patch.diffs.SetItem(patch.diffs.Count - 1, patch.diffs[patch.diffs.Count - 1].Append(postcontext));
        //            } else {
        //              patch.diffs = patch.diffs.Add(new Diff(Operation.EQUAL, postcontext));
        //            }
        //          }
        //          if (!empty) {
        //            patches.Splice(++x, 0, patch);
        //          }
        //        }
        //      }
    }

    [Pure]
    public Rope<Patch> patch_splitMax_pure(IEnumerable<Patch> patches)
    {
        var results = patches.ToRope();
        short patch_size = this.Match_MaxBits;
        for (int x = 0; x < results.Count; x++)
        {
            if (results[x].Length1 <= patch_size)
            {
                continue;
            }

            Patch bigpatch = results[x];

            // Remove the big old patch.
            (_, results) = results.Splice(x--, 1);
            var start1 = bigpatch.Start1;
            var start2 = bigpatch.Start2;
            var precontext = Rope<char>.Empty;
            while (bigpatch.Diffs.Count != 0)
            {
                // Create one of several smaller patches.
                bool empty = true;
                Patch patch = new Patch()
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
                        Diffs = patch.Diffs.Add(new Diff(Operation.EQUAL, precontext))
                    };
                }

                while (bigpatch.Diffs.Count != 0 && patch.Length1 < patch_size - this.Patch_Margin)
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
                            Diffs = patch.Diffs.Add(new Diff(diff_type, diff_text))
                        };
                        start1 += diff_text.Length;
                        empty = false;
                        bigpatch = bigpatch with { Diffs = bigpatch.Diffs.RemoveAt(0) };
                    }
                    else
                    {
                        // Deletion or equality.  Only take as much as we can stomach.
                        diff_text = diff_text.Slice(0, Math.Min(diff_text.Length, patch_size - patch.Length1 - Patch_Margin));
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

                        patch = patch with { Diffs = patch.Diffs.Add(new Diff(diff_type, diff_text)) };
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
                precontext = this.diff_text2(patch.Diffs);
                precontext = precontext.Slice(Math.Max(0, precontext.Length - this.Patch_Margin));

                var postcontext = Rope<char>.Empty;
                // Append the end context for this patch.
                if (diff_text1(bigpatch.Diffs).Length > Patch_Margin)
                {
                    postcontext = diff_text1(bigpatch.Diffs).Slice(0, Patch_Margin);
                }
                else
                {
                    postcontext = diff_text1(bigpatch.Diffs);
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
                        patch = patch with { Diffs = patch.Diffs.SetItem(patch.Diffs.Count - 1, patch.Diffs[patch.Diffs.Count - 1].Append(postcontext)) };
                    }
                    else
                    {
                        patch = patch with { Diffs = patch.Diffs.Add(new Diff(Operation.EQUAL, postcontext)) };
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

    /**
     * Take a list of patches and return a textual representation.
     * @param patches List of Patch objects.
     * @return Text representation of patches.
     */
    public string patch_toText(IEnumerable<Patch> patches)
    {
        StringBuilder text = new StringBuilder();
        foreach (Patch aPatch in patches)
        {
            text.Append(aPatch);
        }

        return text.ToString();
    }

    /**
     * Parse a textual representation of patches and return a List of Patch
     * objects.
     * @param textline Text representation of patches.
     * @return List of Patch objects.
     * @throws ArgumentException If invalid input.
     */
    public List<Patch> patch_fromText(string textline)
    {
        List<Patch> patches = new List<Patch>();
        if (textline.Length == 0)
        {
            return patches;
        }

        string[] text = textline.Split('\n');
        int textPointer = 0;
        Patch patch;
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

            patch = new Patch()
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
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff(Operation.DELETE, line)) };
                }
                else if (sign == '+')
                {
                    // Insertion.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff(Operation.INSERT, line)) };
                }
                else if (sign == ' ')
                {
                    // Minor equality.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff(Operation.EQUAL, line)) };
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

    /**
     * Encodes a string with URI-style % escaping.
     * Compatible with JavaScript's encodeURI function.
     *
     * @param str The string to encode.
     * @return The encoded string.
     */
    public static Rope<char> encodeURI(Rope<char> str)
    {
        // C# is overzealous in the replacements.  Walk back on a few.
        return HttpUtility.UrlEncode(str.ToString())
            .ToRope()
            .Replace('+', ' ').Replace("%20", " ").Replace("%21", "!")
            .Replace("%2a", "*").Replace("%27", "'").Replace("%28", "(")
            .Replace("%29", ")").Replace("%3b", ";").Replace("%2f", "/")
            .Replace("%3f", "?").Replace("%3a", ":").Replace("%40", "@")
            .Replace("%26", "&").Replace("%3d", "=").Replace("%2b", "+")
            .Replace("%24", "$").Replace("%2c", ",").Replace("%23", "#")
            .Replace("%7e", "~");
    }
}