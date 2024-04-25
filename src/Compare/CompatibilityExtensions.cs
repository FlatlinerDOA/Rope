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

namespace Rope.Compare;

using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.Encodings.Web;
using System.Web;

public static class CompatibilityExtensions
{
    private static readonly Rope<char> BlankLineStart1 = "\r\n\r\n".ToRope();
    private static readonly Rope<char> BlankLineStart2 = "\n\n".ToRope();
    private static readonly Rope<char> BlankLineStart3 = "\r\n\n".ToRope();
    private static readonly Rope<char> BlankLineStart4 = "\n\r\n".ToRope();

    private static readonly Rope<char> BlankLineEnd1 = "\n\n".ToRope();
    private static readonly Rope<char> BlankLineEnd2 = "\n\r\n".ToRope();

    private static readonly UrlEncoder DiffEncoder = new DiffUrlEncoder();

    /// <summary>
    /// JScript splice function, removes elements and optionally inserts elements in one operation.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="input">input sequence.</param>
    /// <param name="start">Start Index</param>
    /// <param name="count">Number of elements to remove.</param>
    /// <param name="insertItems">Elements to be inserted.</param>
    /// <returns>A tuple of deleted items and the resulting sequence.</returns>
    [Pure]
    public static (Rope<T> Deleted, Rope<T> Result) Splice<T>(this Rope<T> input, int start, int count, params T[] insertItems) where T : IEquatable<T>
    {
        var deletedRange = input.Slice(start, count);
        input = input.RemoveRange(start, count);
        input = input.InsertRange(start, new Rope<T>(insertItems.AsMemory()));
        return (deletedRange, input);
    }

    /// <summary>
    /// Java substring function 
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="input">Input sequence.</param>
    /// <param name="begin">Start index</param>
    /// <param name="end">End index</param>
    /// <returns></returns>
    [Pure]
    public static Rope<T> JavaSubstring<T>(this Rope<T> input, long begin, long end) where T : IEquatable<T> => input.Slice(begin, end - begin);

    /// <summary>
    /// Alias for <see cref="Rope{T}.AddRange(Rope{T})"/> to aid in replacing StringBuilder.
    /// </summary>
    /// <param name="input">Input sequence.</param>
    /// <param name="append">String to append.</param>
    /// <returns>New sequence.</returns>
    [Pure]
    public static Rope<char> Append(this Rope<char> input, string append) => input.AddRange(append.ToRope());

    [Pure]
    public static Rope<T> Concat<T>(this Rope<T> source, Rope<T> append) where T : IEquatable<T> => source.AddRange(append);

    [Pure]
    public static bool IsBlankLineStart(this Rope<char> str) =>
        str.StartsWith(BlankLineStart1) ||
        str.StartsWith(BlankLineStart2) ||
        str.StartsWith(BlankLineStart3) ||
        str.StartsWith(BlankLineStart4);

    [Pure]
    public static bool IsBlankLineEnd(this Rope<char> str) => str.EndsWith(BlankLineEnd1) || str.EndsWith(BlankLineEnd2);

    /// <summary>
    /// Decodes a string with a very cut down URI-style % escaping.
    /// </summary>
    /// <param name="str">The string to decode.</param>
    /// <returns>The decoded string.</returns>
    [Pure]
    public static Rope<char> DiffDecode(this Rope<char> str) => HttpUtility.UrlDecode(str.Replace("+", "%2B").ToString()).ToRope(); // decode would change all "+" to " "

    /// <summary>
    /// Encodes a sequence with a very cut down URI-style % escaping.
    /// Compatible with JavaScript's EncodeURI function.
    /// </summary>
    /// <param name="items">The sequence to convert to strings and encode.</param>
    /// <returns>The encoded string.</returns>
    [Pure]
    public static Rope<char> DiffEncode<T>(this Rope<T> items) where T : IEquatable<T> => DiffEncoder.Encode(items.ToString()).Replace("%2B", "+", StringComparison.OrdinalIgnoreCase).ToRope();

    /// <summary>
    /// C# is overzealous in the replacements. Walk back on a few.
    /// This is Ok for <see cref="Diff{T}"/> deltas because they are output to text with a length based prefix.
    /// </summary>
    private sealed class DiffUrlEncoder : UrlEncoder
    {
        private static readonly HashSet<int> AllowList = new HashSet<int>(new int[]
        {
            // '+', // Decoder always converts to space, we have to let these get converted to "%27b" and then reverse these ones after the fact.
            ' ',
            '!',
            '*',
            '\'',
            '(',
            ')',
            ';',
            '/',
            '?',
            ':',
            '@',
            '&',
            '=',
            '$',
            ',',
            '#',
            '~',
            0x1F603 // Smile!
        });

        public override int MaxOutputCharactersPerInputCharacter => UrlEncoder.Default.MaxOutputCharactersPerInputCharacter;

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            ReadOnlySpan<char> input = new ReadOnlySpan<char>(text, textLength);
            int idx = 0;

            // Enumerate until we're out of data or saw invalid input
            while (Rune.DecodeFromUtf16(input.Slice(idx), out Rune result, out int charsConsumed) == OperationStatus.Done)
            {
                if (WillEncode(result.Value))
                {
                    // found a char that needs to be escaped
                    break;
                }

                idx += charsConsumed;
            }

            if (idx == input.Length)
            {
                // walked entire input without finding a char which needs escaping
                return -1;
            }

            return idx;
        }

        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            // For anything that needs to be escaped, defer to the default escaper.
            return UrlEncoder.Default.TryEncodeUnicodeScalar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);
        }

        public override bool WillEncode(int unicodeScalar)
        {
            // Allow specific chars and for other chars defer to the default escaper.
            if (AllowList.Contains(unicodeScalar))
            {
                // does not require escaping
                return false;
            } 
            else
            { 
                return UrlEncoder.Default.WillEncode(unicodeScalar);
            }
        }
    }
}
