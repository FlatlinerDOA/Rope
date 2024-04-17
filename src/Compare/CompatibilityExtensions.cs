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
using System.Diagnostics.Contracts;

internal static class CompatibilityExtensions
{
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
}
