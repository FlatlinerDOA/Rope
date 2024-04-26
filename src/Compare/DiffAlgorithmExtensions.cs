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
/// All Diff algorithm extensions to the <see cref="Rope{T}"/> type (and variants thereof such as <see cref="string"/>).
/// </summary>
public static class DiffAlgorithmExtensions
{
    /// <summary>
    /// Find the differences between two texts.
    /// </summary>
    /// <param name="sourceText">Old string to be diffed.</param>
    /// <param name="targetText">New string to be diffed.</param>
    /// <param name="checkLines">
    /// If true, then run a faster slightly less optimal diff by trying line level chunking first. 
    /// Otherwise if false, then don't run a line-level diff first. (Defaults to true).
    /// </param>
    /// <returns>List of Diff objects.</returns>
    [Pure]
    public static Rope<Diff<char>> Diff(this string sourceText, string targetText, bool checkLines = true) =>
        sourceText.ToRope().Diff(targetText.ToRope(), DiffOptions<char>.LineLevel.WithChunking(checkLines));

    /// <summary>
    /// Find the differences between two texts.
    /// </summary>
    /// <param name="text1">Old string to be diffed.</param>
    /// <param name="text2">New string to be diffed.</param>
    /// <param name="options">
    /// Controls amount of time allowed to be taken via  <see cref="DiffOptions{T}.TimeoutSeconds"/>, the timeout just produces a less optimal diff, 
    /// Whether to use a faster algorithm using <see cref="DiffOptions{T}.IsChunkingEnabled"/>,
    /// And what constitutes a maximum edit cost <see cref="DiffOptions{T}.EditCost"/>.
    /// Defaults to use <see cref="DiffOptions{char}.LineLevel"/> when <typeparamref name="T"/> is <see cref="char"/>,
    /// otherwise uses <see cref="DiffOptions{char}.Generic"/>.</param>
    /// <returns>List of Diff objects.</returns>
    [Pure]
    public static Rope<Diff<char>> Diff(this string sourceText, string targetText, DiffOptions<char> options) =>
        sourceText.ToRope().Diff(targetText.ToRope(), options);

    /// <summary>
    /// Find the differences between two texts.
    /// </summary>
    /// <param name="text1">Old string to be diffed.</param>
    /// <param name="text2">New string to be diffed.</param>
    /// <param name="options">
    /// Controls amount of time allowed to be taken via  <see cref="DiffOptions{T}.TimeoutSeconds"/>, the timeout just produces a less optimal diff, 
    /// Whether to use a faster algorithm using <see cref="DiffOptions{T}.IsChunkingEnabled"/>,
    /// And what constitutes a maximum edit cost <see cref="DiffOptions{T}.EditCost"/>.
    /// Defaults to use <see cref="DiffOptions{char}.LineLevel"/> when <typeparamref name="T"/> is <see cref="char"/>,
    /// otherwise uses <see cref="DiffOptions{char}.Generic"/>.</param>
    /// <returns>List of Diff objects.</returns>
    [Pure]
    public static Rope<Diff<T>> Diff<T>(this Rope<T> text1, Rope<T> text2, DiffOptions<T>? options = null) where T : IEquatable<T>
    {
        options ??= DiffOptions<T>.Default;

        // Start a deadline by which time the diff must be complete, cancelling just produces a less optimal diff.
        using var deadline = options.StartTimer();
        return text1.Diff(text2, options, deadline.Cancellation);
    }

    /// <summary>
    /// Find the differences between two texts. Simplifies the problem by
    /// stripping any common prefix or suffix off the texts before diffing.
    /// This overload is thread-safe.
    /// </summary>
    /// <param name="text1">Old string to be diffed.</param>
    /// <param name="text2">New string to be diffed.</param>
    /// <param name="options">
    /// Defines the diffing options.
    /// </param>
    /// <param name="cancel">Cancels the calculation.</param>
    /// <returns></returns>
    [Pure]
    public static Rope<Diff<T>> Diff<T>(this Rope<T> text1, Rope<T> text2, DiffOptions<T> options, CancellationToken cancel) where T : IEquatable<T>
    {
        // Check for null inputs not needed since null can't be passed in C#.
        // Check for equality (speedup).
        if (text1.Equals(text2))
        {
            if (text1.Length != 0)
            {
                return new[] { new Diff<T>(Operation.EQUAL, text1) };
            }

            return Rope<Diff<T>>.Empty;
        }

        // Trim off common prefix (speedup).
        int commonlength = (int)text1.CommonPrefixLength(text2);
        var commonprefix = text1.Slice(0, commonlength);
        text1 = text1.Slice(commonlength);
        text2 = text2.Slice(commonlength);

        // Trim off common suffix (speedup).
        commonlength = (int)text1.CommonSuffixLength(text2);
        var commonsuffix = text1.Slice(text1.Length - commonlength);
        text1 = text1.Slice(0, text1.Length - commonlength);
        text2 = text2.Slice(0, text2.Length - commonlength);

        // Compute the diff on the middle block.
        var diffs = CalculateMiddleDifferences(text1, text2, options, cancel);

        // Restore the prefix and suffix.
        if (commonprefix.Length != 0)
        {
            diffs = diffs.Insert(0, new Diff<T>(Operation.EQUAL, commonprefix));
        }

        if (commonsuffix.Length != 0)
        {
            diffs = diffs.Add(new Diff<T>(Operation.EQUAL, commonsuffix));
        }

        return DiffCleanupMerge(diffs);
    }

    /// <summary>
    /// Given the original text1, and an encoded string which describes the
    /// operations required to transform text1 into text2, compute the full diff.
    /// </summary>
    /// <param name="sourceText">Source string for the diff.</param>
    /// <param name="delta">Delta text.</param>
    /// <returns>Array of Diff objects or null if invalid.</returns>
    /// <exception cref="ArgumentException">If invalid input.</exception>
    [Pure]
    public static Rope<Diff<char>> ConvertDeltaToDiff(this Rope<char> sourceText, Rope<char> delta)
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
                        text = sourceText.Slice(pointer, n);
                        pointer += n;
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        throw new ArgumentException($"Delta length ({pointer}) larger than source text length ({sourceText.Length}).", e);
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

        if (pointer != sourceText.Length)
        {
            throw new ArgumentException("Delta length (" + pointer + ") smaller than source text length (" + sourceText.Length + ").");
        }

        return diffs;
    }

    /// <summary>
    /// Find the differences between two texts. Assumes that the texts do not
    /// have any common prefix or suffix.
    /// </summary>
    /// <param name="text1">Old string to be diffed.</param>
    /// <param name="text2">New string to be diffed.</param>
    /// <param name="options">Configuration options for the diff.</param>
    /// <param name="cancel">Cuts the calculation of the diff short.</param>
    /// <returns>A rope of differences.</returns>
    [Pure]
    internal static Rope<Diff<T>> CalculateMiddleDifferences<T>(Rope<T> text1, Rope<T> text2, DiffOptions<T> options, CancellationToken cancel) where T : IEquatable<T>
    {
        if (text1.Length == 0)
        {
            // Just add some text (speedup).
            return new Rope<Diff<T>>(new[] { new Diff<T>(Operation.INSERT, text2) });
        }

        if (text2.Length == 0)
        {
            // Just delete some text (speedup).
            return new Rope<Diff<T>>(new[] { new Diff<T>(Operation.DELETE, text1) });
        }

        var longtext = text1.Length > text2.Length ? text1 : text2;
        var shorttext = text1.Length > text2.Length ? text2 : text1;
        var i = longtext.IndexOf(shorttext);
        if (i != -1)
        {
            // Shorter text is inside the longer text (speedup).
            Operation op = (text1.Length > text2.Length) ? Operation.DELETE : Operation.INSERT;
            return new Rope<Diff<T>>(new[]
            {
                new Diff<T>(op, longtext.Slice(0, i)),
                new Diff<T>(Operation.EQUAL, shorttext),
                new Diff<T>(op, longtext.Slice(i + shorttext.Length))
            });
        }

        if (shorttext.Length == 1)
        {
            // Single character string.
            // After the previous speedup, the character can't be an equality.
            return new Rope<Diff<T>>(new[]
            {
                new Diff<T>(Operation.DELETE, text1),
                new Diff<T>(Operation.INSERT, text2)
            });
        }

        // Check to see if the problem can be split in two.
        var hmOrNull = DiffHalfMatch(text1, text2, options);
        if (hmOrNull is HalfMatch<T> hm)
        {
            // A half-match was found, send both pairs off for separate processing.
            var diffs_a = Diff(hm.Text1Prefix, hm.Text2Prefix, options, cancel);
            diffs_a = diffs_a + new Diff<T>(Operation.EQUAL, hm.Common);
            var diffs_b = Diff(hm.Text1Suffix, hm.Text2Suffix, options, cancel);
            return diffs_a + diffs_b;
        }

        if (options.IsChunkingEnabled && text1.Length > 100 && text2.Length > 100)
        {
            return ComputeChunkLevelDifferences(text1, text2, options, cancel);
        }

        return DiffBisect(text1, text2, options, cancel);
    }

    /// <summary>
    /// Do a quick chunk-level diff on both sequences, then rediff the parts for
    /// greater accuracy.
    /// This speedup can produce non-minimal diffs.
    /// </summary>
    /// <param name="text1">Old string to be diffed.</param>
    /// <param name="text2">New string to be diffed.</param>
    /// <param name="cancel">Cancellation to abort the diff.</param>
    /// <returns>List of Diff objects.</returns>
    [Pure]
    internal static Rope<Diff<T>> ComputeChunkLevelDifferences<T>(Rope<T> text1, Rope<T> text2, DiffOptions<T> options, CancellationToken cancel) where T : IEquatable<T>
    {
        // Scan the text on a line-by-line basis first.
        (var chars1, var chars2, var linearray) = DiffChunksToChars(text1, text2, options);
        var optionsWithoutCheckLines = new DiffOptions<char>(0, 0, false, '\n');
        var charDiffs = Diff(chars1, chars2, optionsWithoutCheckLines, cancel);

        // Convert the diff back to original text.
        var diffs = ConvertCharsToChunksFast(charDiffs, linearray);

        // Eliminate freak matches (e.g. blank lines)
        diffs = DiffCleanupSemantic(diffs, cancel);

        // Rediff any replacement blocks, this time character-by-character.
        // Add a dummy entry at the end.
        diffs = diffs.Add(new Diff<T>(Operation.EQUAL, string.Empty));
        int pointer = 0;
        int count_delete = 0;
        int count_insert = 0;
        var text_delete = Rope<T>.Empty;
        var text_insert = Rope<T>.Empty;
        while (pointer < diffs.Count)
        {
            //Debug.Assert(!cancel.IsCancellationRequested, "Cancelled diff_lineMode");

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
                        var subDiff = Diff(text_delete, text_insert, options.WithChunking(false), cancel);
                        diffs = diffs.InsertRange(pointer, subDiff);
                        pointer = pointer + subDiff.Count;
                    }
                    count_insert = 0;
                    count_delete = 0;
                    text_delete = Rope<T>.Empty;
                    text_insert = Rope<T>.Empty;
                    break;
            }
            pointer++;
        }

        diffs = diffs.RemoveAt(diffs.Count - 1);  // Remove the dummy entry at the end.
        return diffs;
    }

    internal static Rope<Diff<T>> DiffBisect<T>(this Rope<T> text1Rope, Rope<T> text2Rope, DiffOptions<T> options, CancellationToken cancel) where T : IEquatable<T>
    {
        // Cache the text lengths to prevent multiple calls.
        ////File.AppendAllText(@"D:\ChizDev\Rope\benchmarks\statslog.csv", $"Bisect,{text1Rope.Length},{text1Rope.Depth},{text2Rope.Length},{text2Rope.Depth}\n");
        var text1Memory = text1Rope.ToMemory();
        var text2Memory = text2Rope.ToMemory();
        var text1 = text1Memory.Span;
        var text2 = text2Memory.Span;
        var text1_length = text1.Length;
        var text2_length = text2.Length;
        var max_d = (int)(text1_length + text2_length + 1) / 2;
        var v_offset = max_d;
        var v_length = 2 * max_d;
        
        var v1 = new int[v_length].AsSpan();
        var v2 = new int[v_length].AsSpan();
        v1.Fill(-1);
        v2.Fill(-1);
        v1[v_offset + 1] = 0;
        v2[v_offset + 1] = 0;
        var delta = (int)(text1_length - text2_length);
        // If the total number of characters is odd, then the front path will
        // collide with the reverse path.
        bool front = (delta % 2 != 0);
        // Offsets for start and end of k loop.
        // Prevents mapping of space beyond the grid.
        int k1start = 0;
        int k1end = 0;
        int k2start = 0;
        int k2end = 0;
        for (int d = 0; d < max_d; d++)
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
                int x1;
                if (k1 == -d || k1 != d && v1[k1_offset - 1] < v1[k1_offset + 1])
                {
                    x1 = v1[k1_offset + 1];
                }
                else
                {
                    x1 = v1[k1_offset - 1] + 1;
                }

                int y1 = x1 - k1;
                while (x1 < text1_length && y1 < text2_length && text1[x1].Equals(text2[y1]))
                {
                    x1++;
                    y1++;
                }

                // EXPERIMENTAL: Slice + CommonPrefixLength, seems to allocate GB's!??
                // if (x1 < text1_length && y1 < text2_length)
                // {
                //     var prefix = (int)text1[x1..].CommonPrefixLength(text2[y1..]);
                //     x1 += prefix;
                //     y1 += prefix;
                // }

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
                            return DiffBisectSplit(text1Rope, text2Rope, x1, y1, options, cancel);
                        }
                    }
                }
            }

            // Walk the reverse path one step.
            for (var k2 = -d + k2start; k2 <= d - k2end; k2 += 2)
            {
                var k2_offset = v_offset + k2;
                int x2;
                if (k2 == -d || k2 != d && v2[k2_offset - 1] < v2[k2_offset + 1])
                {
                    x2 = v2[k2_offset + 1];
                }
                else
                {
                    x2 = v2[k2_offset - 1] + 1;
                }

                var y2 = x2 - k2;
                while (x2 < text1_length && y2 < text2_length && text1[text1_length - x2 - 1].Equals(text2[text2_length - y2 - 1]))
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
                        x2 = (int)(text1_length - v2[k2_offset]);
                        if (x1 >= x2)
                        {
                            // Overlap detected.
                            return DiffBisectSplit(text1Rope, text2Rope, x1, y1, options, cancel);
                        }
                    }
                }
            }
        }

        // Diff took too long and hit the deadline or
        // number of diffs equals number of characters, no commonality at all.
        return new Rope<Diff<T>>(new[] { new Diff<T>(Operation.DELETE, text1Rope), new Diff<T>(Operation.INSERT, text2Rope) });
    }

    /// <summary>
    /// Given the location of the 'middle snake', split the diff in two parts
    /// and recurse.
    /// </summary>
    /// <typeparam name="T">Type of items in the sequence being compared.</typeparam>
    /// <param name="text1">Old string to be diffed.</param>
    /// <param name="text2"> New string to be diffed.</param>
    /// <param name="x">Index of split point in text1.</param>
    /// <param name="y">Index of split point in text2.</param>
    /// <param name="options">Settings for how the diff should be performed.</param>
    /// <param name="cancel">Cancelling cuts the diff operation short.</param>
    /// <returns>Sequence of  Diff objects.</returns>
    [Pure]
    internal static Rope<Diff<T>> DiffBisectSplit<T>(this Rope<T> text1, Rope<T> text2, long x, long y, DiffOptions<T> options, CancellationToken cancel) where T : IEquatable<T>
    {
        var text1a = text1.Slice(0, x);
        var text2a = text2.Slice(0, y);
        var text1b = text1.Slice(x);
        var text2b = text2.Slice(y);

        // Compute both diffs serially.
        var optionsWithoutCheckLines = options.WithChunking(false);
        var diffs = Diff(text1a, text2a, optionsWithoutCheckLines, cancel);
        var diffsb = Diff(text1b, text2b, optionsWithoutCheckLines, cancel);
        return diffs.Concat(diffsb);
    }


    /// <summary>
    /// Split two texts into a list of strings.Reduce the texts to a string of
    /// hashes where each Unicode character represents one line.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="text1">First string.</param>
    /// <param name="text2">Second string.</param>
    /// <param name="options"></param>
    /// <returns>Three element tuple, containing the encoded text1, the
    /// encoded text2 and the List of unique strings. The zeroth element
    /// of the List of unique strings is intentionally blank.</returns>
    [Pure]
    internal static (Rope<char> Text1Encoded, Rope<char> Text2Encoded, Rope<Rope<T>> Lines) DiffChunksToChars<T>(this Rope<T> text1, Rope<T> text2, DiffOptions<T> options) where T : IEquatable<T>
    {
        var lineArray = Rope<Rope<T>>.Empty;
        var lineHash = new Dictionary<Rope<T>, int>();
        // e.g. linearray[4] == "Hello\n"
        // e.g. linehash.get("Hello\n") == 4

        // "\x00" is a valid character, but various debuggers don't like it.
        // So we'll insert a junk entry to avoid generating a null character.
        lineArray = lineArray.Add(Rope<T>.Empty);
        // var chars1 = Munge2(text1, ref lineArray, lineHash, 40000);
        // var chars2 = Munge2(text1, ref lineArray, lineHash, 65535);

        // Allocate 2/3rds of the space for text1, the rest for text2.
        (var chars1, lineArray) = AccumulateChunksIntoChars(text1, lineArray, lineHash, 40000, options);
        (var chars2, lineArray) = AccumulateChunksIntoChars(text2, lineArray, lineHash, 65535, options);
        return (chars1, chars2, lineArray.Balanced());
    }

    ////// Experimental: Attempt at faster performance than diff_linesToCharsMunge_pure
    ////private static Rope<T> Munge2(Rope<T> text1, ref Rope<Rope<T>> lineArray, Dictionary<Rope<T>, int> lineHash, int maxLines)
    ////{
    ////    var chars = Rope<T>.Empty;
    ////    long consumed = 0;
    ////    foreach (var line in text1.Split('\n'))
    ////    {
    ////        var e = lineHash.GetValueOrDefault(line, -1);
    ////        if (e == -1)
    ////        {
    ////            if (lineArray.Count == maxLines)
    ////            {
    ////                e = lineHash.Count;
    ////                lineArray += text1.Slice(consumed);
    ////                break;
    ////            }

    ////            lineArray += line;
    ////            lineHash.Add(line, e);
    ////        }

    ////        chars += (char)e;
    ////        consumed += line.Length;
    ////    }

    ////    return chars;
    ////}

    /// <summary>
    /// Split a sequence into a list of chunks (e.g. lines). Reduce the texts to a sequence of
    /// hashes where each Unicode character represents one chunk.
    /// </summary>
    /// <param name="text">String to encode.</param>
    /// <param name="lineArray">List of unique strings.</param>
    /// <param name="lineHash">Map of strings to indices. NOTE: This is mutated and added to.</param>
    /// <param name="maxLines"> Maximum length of lineArray.</param>
    /// <returns>Encoded string.</returns>
    internal static (Rope<char> Chars, Rope<Rope<T>> LineArray) AccumulateChunksIntoChars<T>(this Rope<T> text, Rope<Rope<T>> lineArray, Dictionary<Rope<T>, int> lineHash, int maxLines, DiffOptions<T> options) where T : IEquatable<T>
    {
        long lineStart = 0;
        long lineEnd = -1;
        var line = Rope<T>.Empty;
        var chars = Rope<char>.Empty;
        var s = options.ChunkSeparator;

        // Walk the text, pulling out a Substring for each line.
        // text.split('\n') would would temporarily double our memory footprint.
        // Modifying text would create many large strings to garbage collect.
        while (lineEnd < text.Length - 1)
        {
            lineEnd = text.IndexOf(s, lineStart);
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
                lineHash.Add(line, lineArray.Count - 1);
                chars = chars.Add((char)(lineArray.Count - 1));
            }

            lineStart = lineEnd + 1;
        }

        return (chars, lineArray);
    }

    [Pure]
    internal static Rope<Diff<T>> ConvertCharsToChunks<T>(this Rope<Diff<char>> diffs, Rope<Rope<T>> lineArray) where T : IEquatable<T>
    {
        var result = Rope<Diff<T>>.Empty;
        foreach (var diff in diffs)
        {
            var text = Rope<T>.Empty;
            for (int j = 0; j < diff.Text.Length; j++)
            {
                text = text.AddRange(lineArray[(int)diff.Text[j]]);
            }

            result = result.Add(new(diff.Operation, text));
        }

        return result;
    }

    [Pure]
    internal static Rope<Diff<T>> ConvertCharsToChunksFast<T>(this Rope<Diff<char>> diffs, Rope<Rope<T>> lineArray) where T : IEquatable<T>
    {
        var q = from diff in diffs
                let text = (from c in diff.Text
                            select lineArray[c]).Combine()
                select new Diff<T>(diff.Operation, text);
        return q.ToRope();
    }

    /// <summary>
    /// Reduce the number of edits by eliminating semantically trivial
    /// equalities.
    /// </summary>
    /// <param name="diffs">List of Diff objects.</param>
    /// <param name="cancellation">Cancellation</param>
    /// <returns>A more semantically clean list of diffs.</returns>
    [Pure]
    internal static Rope<Diff<T>> DiffCleanupSemantic<T>(this Rope<Diff<T>> diffs, CancellationToken cancellation = default) where T : IEquatable<T>
    {
        bool changes = false;
        // Stack of indices where equalities are found.
        Stack<long> equalities = new Stack<long>();

        // Always equal to equalities[equalitiesLength-1][1]
        var lastEquality = Rope<T>.Empty;
        long pointer = 0;  // Index of current position.
                           // Number of characters that changed prior to the equality.
        long length_insertions1 = 0;
        long length_deletions1 = 0;
        // Number of characters that changed after the equality.
        long length_insertions2 = 0;
        long length_deletions2 = 0;
        while (pointer < diffs.Count)
        {
            //Debug.Assert(!cancellation.IsCancellationRequested, "Cancelled diff_cleanupSemantic_pure #1");

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
                    diffs = diffs.Insert(equalities.Peek(), new Diff<T>(Operation.DELETE, lastEquality));

                    // Change second copy to insert.
                    diffs = diffs.SetItem(equalities.Peek() + 1, new Diff<T>(Operation.INSERT, diffs[equalities.Peek() + 1].Text));

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
                    lastEquality = Rope<T>.Empty;
                    changes = true;
                }
            }

            pointer++;
        }

        // Normalize the diff.
        if (changes)
        {
            diffs = DiffCleanupMerge(diffs);
        }

        diffs = DiffCleanupSemanticLossless(diffs, cancellation);

        // Find any overlaps between deletions and insertions.
        // e.g: <del>abcxxx</del><ins>xxxdef</ins>
        //   -> <del>abc</del>xxx<ins>def</ins>
        // e.g: <del>xxxabc</del><ins>defxxx</ins>
        //   -> <ins>def</ins>xxx<del>abc</del>
        // Only extract an overlap if it is as big as the edit ahead or behind it.
        pointer = 1;
        while (pointer < diffs.Count)
        {
            //Debug.Assert(!cancellation.IsCancellationRequested, "Cancelled diff_cleanupSemantic_pure #2");

            if (diffs[pointer - 1].Operation == Operation.DELETE && diffs[pointer].Operation == Operation.INSERT)
            {
                var deletion = diffs[pointer - 1].Text;
                var insertion = diffs[pointer].Text;
                int overlap_length1 = deletion.CommonOverlapLength(insertion);
                int overlap_length2 = insertion.CommonOverlapLength(deletion);
                if (overlap_length1 != 0 && overlap_length1 >= overlap_length2)
                {
                    if (overlap_length1 >= deletion.Length / 2.0 ||
                        overlap_length1 >= insertion.Length / 2.0)
                    {
                        // Overlap found.
                        // Insert an equality and trim the surrounding edits.
                        diffs = diffs.Insert(pointer, new Diff<T>(Operation.EQUAL, insertion.Slice(0, overlap_length1)));
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
                        diffs = diffs.Insert(pointer, new Diff<T>(Operation.EQUAL, deletion.Slice(0, overlap_length2)));
                        diffs = diffs.SetItem(pointer - 1, new Diff<T>(Operation.INSERT, insertion.Slice(0, insertion.Length - overlap_length2)));
                        diffs = diffs.SetItem(pointer + 1, new Diff<T>(Operation.DELETE, deletion.Slice(overlap_length2)));
                        pointer++;
                    }
                }

                pointer++;
            }

            pointer++;
        }

        return diffs;
    }

    /// <summary>
    /// Given two strings, compute a score representing whether the internal
    /// boundary falls on logical boundaries. Scores range from 6 (best) to 0 (worst).
    /// </summary>
    /// <param name="one">First string.</param>
    /// <param name="two">Second string.</param>
    /// <returns>An int between 0 and 6.</returns>
    [Pure]
    internal static int DiffCleanupSemanticScore<T>(Rope<T> one, Rope<T> two) where T : IEquatable<T>
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
        if (one is Rope<char> oneChar && two is Rope<char> twoChar)
        {
            var char1 = oneChar[one.Length - 1];
            var char2 = twoChar[0];
            bool nonAlphaNumeric1 = !char.IsLetterOrDigit(char1);
            bool nonAlphaNumeric2 = !char.IsLetterOrDigit(char2);
            bool whitespace1 = nonAlphaNumeric1 && char.IsWhiteSpace(char1);
            bool whitespace2 = nonAlphaNumeric2 && char.IsWhiteSpace(char2);
            bool lineBreak1 = whitespace1 && char.IsControl(char1);
            bool lineBreak2 = whitespace2 && char.IsControl(char2);
            bool blankLine1 = lineBreak1 && oneChar.IsBlankLineEnd();
            bool blankLine2 = lineBreak2 && twoChar.IsBlankLineStart();

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
        }

        // Each port of this function behaves slightly differently due to
        // subtle differences in each language's definition of things like
        // 'whitespace'.  Since this function's purpose is largely cosmetic,
        // the choice has been made to use each language's native features
        // rather than force total conformity.
        return 0;
    }

    /// <summary>
    /// Look for single edits surrounded on both sides by equalities
    /// which can be shifted sideways to align the edit to a word boundary.
    /// e.g: The c<ins>at c</ins>ame. -> The<ins> cat </ins>came.
    /// </summary>
    /// <param name="inputDiffs">List of Diff objects.</param>
    /// <param name="cancellation">Cancellation.</param>
    /// <returns>A losslessly cleaner list of diffs.</returns>
    [Pure]
    internal static Rope<Diff<T>> DiffCleanupSemanticLossless<T>(this Rope<Diff<T>> inputDiffs, CancellationToken cancellation = default) where T : IEquatable<T>
    {
        var diffs = inputDiffs;
        int pointer = 1;

        // Intentionally ignore the first and last element (don't need checking).
        while (pointer < diffs.Count - 1)
        {
            //Debug.Assert(!cancellation.IsCancellationRequested, "Cancelled diff_cleanupSemanticLossless_pure");
            if (diffs[pointer - 1].Operation == Operation.EQUAL && diffs[pointer + 1].Operation == Operation.EQUAL)
            {
                // This is a single edit surrounded by equalities.
                var equality1 = diffs[pointer - 1].Text;
                var edit = diffs[pointer].Text;
                var equality2 = diffs[pointer + 1].Text;

                // First, shift the edit as far left as possible.
                var commonOffset = equality1.CommonSuffixLength(edit);
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
                int bestScore = DiffCleanupSemanticScore(equality1, edit) + DiffCleanupSemanticScore(edit, equality2);
                while (edit.Length != 0 && equality2.Length != 0 && edit[0].Equals(equality2[0]))
                {
                    equality1 = equality1.Concat(edit.Slice(0, 1));
                    edit = edit.Slice(1).Concat(equality2.Slice(0, 1));
                    equality2 = equality2.Slice(1);
                    int score = DiffCleanupSemanticScore(equality1, edit) + DiffCleanupSemanticScore(edit, equality2);
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

                    diffs = diffs.SetItem(pointer, new Diff<T>(diffs[pointer].Operation, bestEdit));
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

    [Pure]
    internal static Rope<Diff<T>> DiffCleanupEfficiency<T>(this Rope<Diff<T>> diffs, DiffOptions<T> options) where T : IEquatable<T>
    {
        bool changes = false;

        // Stack of indices where equalities are found.
        var equalities = new Stack<long>();

        // Always equal to equalities[equalitiesLength-1][1]
        var lastEquality = Rope<T>.Empty;

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
                if (diffs[pointer].Text.Length < options.EditCost && (post_ins || post_del))
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
                    lastEquality = ReadOnlyMemory<T>.Empty;
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
                    || ((lastEquality.Length < options.EditCost / 2)
                    && ((pre_ins ? 1 : 0) + (pre_del ? 1 : 0) + (post_ins ? 1 : 0)
                    + (post_del ? 1 : 0)) == 3)))
                {
                    // Duplicate record.
                    diffs = diffs.Insert(equalities.Peek(), new Diff<T>(Operation.DELETE, lastEquality));
                    // Change second copy to insert.
                    diffs = diffs.SetItem(equalities.Peek() + 1, diffs[equalities.Peek() + 1].WithOperation(Operation.INSERT));
                    equalities.Pop();  // Throw away the equality we just deleted.
                    lastEquality = ReadOnlyMemory<T>.Empty;
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
            diffs = DiffCleanupMerge(diffs);
        }

        return diffs;
    }

    /// <summary>
    /// Reorder and merge like edit sections.Merge equalities.
    /// Any edit section can move as long as it doesn't cross an equality.
    /// </summary>
    /// <param name="diffs">List of Diff objects.</param>
    /// <returns>List of cleaned diffs.</returns>
    [Pure]
    internal static Rope<Diff<T>> DiffCleanupMerge<T>(this Rope<Diff<T>> diffs) where T : IEquatable<T>
    {
        // Add a dummy entry at the end.
        diffs = diffs.Add(new Diff<T>(Operation.EQUAL, Rope<T>.Empty));
        int pointer = 0;
        int count_delete = 0;
        int count_insert = 0;
        var text_delete = Rope<T>.Empty;
        var text_insert = Rope<T>.Empty;
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
                            commonlength = (int)text_insert.CommonPrefixLength(text_delete);
                            if (commonlength != 0)
                            {
                                var t = pointer - count_delete - count_insert - 1;
                                if (t >= 0 && diffs[t].Operation == Operation.EQUAL)
                                {
                                    diffs = diffs.SetItem(t, diffs[t].Append(text_insert.Slice(0, commonlength)));
                                }
                                else
                                {
                                    diffs = diffs.Insert(0, new Diff<T>(Operation.EQUAL, text_insert.Slice(0, commonlength)));
                                    pointer++;
                                }

                                text_insert = text_insert.Slice(commonlength);
                                text_delete = text_delete.Slice(commonlength);
                            }

                            // Factor out any common suffixies.
                            commonlength = (int)text_insert.CommonSuffixLength(text_delete);
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
                            (_, diffs) = diffs.Splice(pointer, 0, new Diff<T>(Operation.DELETE, text_delete));
                            pointer++;
                        }

                        if (text_insert.Length != 0)
                        {
                            (_, diffs) = diffs.Splice(pointer, 0, new Diff<T>(Operation.INSERT, text_insert));
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
                    text_delete = Rope<T>.Empty;
                    text_insert = Rope<T>.Empty;
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
            diffs = DiffCleanupMerge(diffs);
        }

        return diffs;
    }

    /// <summary>
    /// Do the two texts share a Substring which is at least half the length of
    /// the longer text? This speedup can produce non-minimal diffs.
    /// </summary>
    /// <param name="text1">First string</param>
    /// <param name="text2">Second string</param>
    /// <returns>Five element String array containing:
    /// 0 - the prefix of text1,
    /// 1 - the suffix of text1,
    /// 2 - the prefix of text2,
    /// 3 - the suffix of text2
    /// 4 - and the common middle.
    /// Or null if there was no match.</returns>
    [Pure]
    internal static HalfMatch<T>? DiffHalfMatch<T>(this Rope<T> text1, Rope<T> text2, DiffOptions<T> options) where T : IEquatable<T>
    {
        if (options.TimeoutSeconds <= 0)
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
        var hm1OrNull = DiffHalfMatchAt(longtext, shorttext, (longtext.Length + 3) / 4);
        
        // Check again based on the third quarter.
        var hm2OrNull = DiffHalfMatchAt(longtext, shorttext, (longtext.Length + 1) / 2);

        HalfMatch<T> hm;
        if (hm1OrNull is HalfMatch<T> hm1)
        {
            if (hm2OrNull is HalfMatch<T> hm2)
            {
                // Both matched.  Select the longest.
                hm = hm1.Common.Length > hm2.Common.Length ? hm1 : hm2;
            }
            else
            {
                hm = hm1;
            }
        }
        else if (hm2OrNull is HalfMatch<T> hm2)
        {
            hm = hm2;
        }
        else 
        {
            // Both null;
            return null;
        }        

        // A half-match was found, sort out the return data.
        if (text1.Length > text2.Length)
        {
            return hm;
        }
        else
        {
            return hm.Swap();
        }
    }

    /// <summary>
    /// Does a Substring of shorttext exist within longtext such that the
    /// Substring is at least half the length of longtext?
    /// </summary>
    /// <param name="longtext">Longer string.</param>
    /// <param name="shorttext">Shorter string.</param>
    /// <param name="startIndex">Start index of quarter length Substring within longtext.</param>
    /// <returns>Five element record, containing the prefix of longtext, the
    /// suffix of longtext, the prefix of shorttext, the suffix of shorttext
    /// and the common middle. Otherwise null if there was no match.</returns>
    [Pure]
    internal static HalfMatch<T>? DiffHalfMatchAt<T>(Rope<T> longtext, Rope<T> shorttext, long startIndex) where T : IEquatable<T>
    {
        // Start with a 1/4 length Substring at position i as a seed.
        var seed = longtext.Slice(startIndex, longtext.Length / 4);
        long j = -1;
        var best_common = Rope<T>.Empty;
        var best_longtext_a = Rope<T>.Empty;
        var best_longtext_b = Rope<T>.Empty;
        var best_shorttext_a = Rope<T>.Empty;
        var best_shorttext_b = Rope<T>.Empty;
        while (j < shorttext.Length && (j = shorttext.IndexOf(seed, j + 1)) != -1)
        {
            long prefixLength = longtext.Slice(startIndex).CommonPrefixLength(shorttext.Slice(j));
            long suffixLength = longtext.Slice(0, startIndex).CommonSuffixLength(shorttext.Slice(0, j));
            if (best_common.Length < suffixLength + prefixLength)
            {
                best_common = shorttext.Slice(j - suffixLength, suffixLength).AddRange(shorttext.Slice(j, prefixLength));
                best_longtext_a = longtext.Slice(0, startIndex - suffixLength);
                best_longtext_b = longtext.Slice(startIndex + prefixLength);
                best_shorttext_a = shorttext.Slice(0, j - suffixLength);
                best_shorttext_b = shorttext.Slice(j + prefixLength);
            }
        }

        if (best_common.Length * 2 >= longtext.Length)
        {
            return new HalfMatch<T>(
                best_longtext_a,
                best_longtext_b,
                best_shorttext_a,
                best_shorttext_b,
                best_common);
        }
        else
        {
            return null;
        }
    }
}