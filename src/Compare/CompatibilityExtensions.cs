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
    private static readonly UrlEncoder DiffEncoder = new DiffUrlEncoder();

    // JScript splice function
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
    public static string JavaSubstring(this string s, int begin, int end) => s.Substring(begin, end - begin);

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

    private static readonly Rope<char> BlankLineStart1 = "\r\n\r\n".ToRope();
    private static readonly Rope<char> BlankLineStart2 = "\n\n".ToRope();
    private static readonly Rope<char> BlankLineStart3 = "\r\n\n".ToRope();
    private static readonly Rope<char> BlankLineStart4 = "\n\r\n".ToRope();

    private static readonly Rope<char> BlankLineEnd1 = "\n\n".ToRope();
    private static readonly Rope<char> BlankLineEnd2 = "\n\r\n".ToRope();

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
    /// Encodes a string with a very cut down URI-style % escaping.
    /// Compatible with JavaScript's EncodeURI function.
    /// </summary>
    /// <param name="str">The string to encode.</param>
    /// <returns>The encoded string.</returns>
    [Pure]
    public static Rope<char> DiffEncode<T>(this Rope<T> str) where T : IEquatable<T> => DiffEncoder.Encode(str.ToString()).Replace("%2B", "+", StringComparison.OrdinalIgnoreCase).ToRope();

    /// <summary>
    /// C# is overzealous in the replacements. Walk back on a few.
    /// This is ok for <see cref="Diff"/> deltas because they are output to text with a length based prefix.
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
