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
using System.Diagnostics;
using System.Diagnostics.Contracts;

/// <summary>
/// Struct representing a single patch operation.
/// </summary>
/// <typeparam name="T">The items being patched, typically <see cref="char"/> when patching strings.</typeparam>
public readonly record struct Patch<T>() where T : IEquatable<T>
{
    public Rope<Diff<T>> Diffs { get; init; } = Rope<Diff<T>>.Empty;
    
    public long Start1 { get; init; }
    
    public long Length1 { get; init; }

    public long Start2 { get; init; }

    public long Length2 { get; init; }

    /**
     * Emulate GNU diff's format.
     * Header: @@ -382,8 +481,9 @@
     * Indices are printed as 1-based, not 0-based.
     * @return The GNU diff string.
     */
    public override string ToString() => this.ToCharRope().ToString();

    /// <summary>
    /// Internal version that constructs a rope, internal as it is only compatible when <typeparamref name="T"/> is of type <see cref="char"/>.
    /// </summary>
    /// <returns>The constructed string as a <see cref="Rope{char}"/>.</returns>
    internal Rope<char> ToCharRope()
    {
        Debug.Assert(typeof(T) == typeof(char), "This overload is only compatible with `char` type,\nuse ToCharRope(Func<T, Rope<char>> itemToString) passing a\nfunction to convert each item to a string.");
        Rope<char> coords1, coords2;
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
        text = text.Append("@@ -").AddRange(coords1).Append(" +").AddRange(coords2).Append(" @@\n");

        // Escape the body of the patch with %xx notation.
        foreach (var aDiff in this.Diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    text = text.Add('+');
                    break;
                case Operation.Delete:
                    text = text.Add('-');
                    break;
                case Operation.Equal:
                    text = text.Add(' ');
                    break;
            }

            text = text.AddRange(aDiff.Items.ToString().ToRope().DiffEncode()).Append("\n");
        }

        return text;
    }

    /// <summary>
    /// Joins items by a separator and then includes them in a text patch.
    /// </summary>
    /// <param name="itemToString">A function to convert a single item into text.</param>
    /// <param name="separator">The separator between items, defaults to ~</param>
    /// <returns>The constructed string as a <see cref="Rope{char}"/>.</returns>
    public Rope<char> ToSeparatedCharRope(Func<T, Rope<char>> itemToString, char separator = '~')
    {
        /////Contract.Assert(typeof(T) != typeof(char), "This overload is only compatible with non `char` types, use ToString() instead.");

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
        foreach (var aDiff in this.Diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    text = text.Add('+');
                    break;
                case Operation.Delete:
                    text = text.Add('-');
                    break;
                case Operation.Equal:
                    text = text.Add(' ');
                    break;
            }
            
            text = text.AddRange(aDiff.Items.DiffEncode(itemToString, separator)).Append("\n");
        }

        return text;
    }
}
