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
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using System.Web;

/// <summary>
/// Static functions for creating or parsing out a list of patches.
/// </summary>
public static partial class Patches
{
    private static readonly Regex PatchHeaderPattern = CreatePatchHeaderPattern();

    [GeneratedRegex("^@@ -(\\d+),?(\\d*) \\+(\\d+),?(\\d*) @@$")]
    private static partial Regex CreatePatchHeaderPattern();

    /// <summary>
    /// Parse a textual representation of patches and return a list of <see cref="Patch{T}"/>.
    /// objects.
    /// </summary>
    /// <typeparam name="T">The item types to be deserialized or parsed.</typeparam>
    /// <param name="patchText">Text representation of patches.</param>
    /// <returns>List of Patch objects.</returns>
    /// <exception cref="ArgumentException">Thrown if invalid input.</exception>
    public static Rope<Patch<T>> Parse<T>(Rope<char> patchText, Func<Rope<char>, T> parseItem, char separator = '~') where T : IEquatable<T>
    {
        var patches = Rope<Patch<T>>.Empty;
        if (patchText.Length == 0)
        {
            return patches;
        }

        var text = patchText.Split('\n').ToRope();
        long textPointer = 0;
        Patch<T> patch;
        Match m;
        char sign;
        Rope<char> line = Rope<char>.Empty;
        while (textPointer < text.Length)
        {
            var pointerText = text[textPointer].ToString();
            m = PatchHeaderPattern.Match(pointerText);
            if (!m.Success)
            {
                throw new ArgumentException("Invalid patch string: " + pointerText);
            }

            patch = new Patch<T>()
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

                line = text[textPointer].Slice(1);
                line = line.Replace("+", "%2b");
                line = HttpUtility.UrlDecode(line.ToString());
                if (sign == '-')
                {
                    // Deletion.
                    var items = line.Split(separator).Select(i => parseItem(i)).ToRope();
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<T>(Operation.Delete, items)) };
                }
                else if (sign == '+')
                {
                    // Insertion.
                    var items = line.Split(separator).Select(i => parseItem(i)).ToRope();
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<T>(Operation.Insert, items)) };
                }
                else if (sign == ' ')
                {
                    // Minor equality.
                    var items = line.Split(separator).Select(i => parseItem(i)).ToRope();
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<T>(Operation.Equal, items)) };
                }
                else if (sign == '@')
                {
                    // Start of next patch.
                    break;
                }
                else
                {
                    // WTF?
                    throw new ArgumentException("Invalid patch mode '" + sign + "' in: " + line.ToString());
                }

                textPointer++;
            }

            patches = patches.Add(patch);

        }

        return patches;
    }

    /// <summary>
    /// Parse a textual representation of patches and return a List of Patch
    /// objects.
    /// </summary>
    /// <param name="patchText">Text representation of patches.</param>
    /// <returns>List of Patch objects.</returns>
    /// <exception cref="ArgumentException">Thrown if invalid input.</exception>
    public static Rope<Patch<char>> Parse(Rope<char> patchText)
    {
        var patches = Rope<Patch<char>>.Empty;
        if (patchText.Length == 0)
        {
            return patches;
        }

        var text = patchText.Split('\n').ToRope();
        long textPointer = 0;
        Patch<char> patch;
        Match m;
        char sign;
        Rope<char> line = Rope<char>.Empty;
        while (textPointer < text.Length)
        {
            var pointerText = text[textPointer].ToString();
            m = PatchHeaderPattern.Match(pointerText);
            if (!m.Success)
            {
                throw new ArgumentException("Invalid patch string: " + pointerText);
            }

            patch = new Patch<char>()
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

                line = text[textPointer].Slice(1);
                line = line.Replace("+", "%2b");
                line = HttpUtility.UrlDecode(line.ToString());
                if (sign == '-')
                {
                    // Deletion.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(Operation.Delete, line)) };
                }
                else if (sign == '+')
                {
                    // Insertion.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(Operation.Insert, line)) };
                }
                else if (sign == ' ')
                {
                    // Minor equality.
                    patch = patch with { Diffs = patch.Diffs.Add(new Diff<char>(Operation.Equal, line)) };
                }
                else if (sign == '@')
                {
                    // Start of next patch.
                    break;
                }
                else
                {
                    // WTF?
                    throw new ArgumentException("Invalid patch mode '" + sign + "' in: " + line.ToString());
                }

                textPointer++;
            }

            patches = patches.Add(patch);

        }

        return patches;
    }

    /// <summary>
    /// Compute a list of patches to turn <paramref name="source"/> into <paramref name="target"/>.
    /// A set of diffs will be computed.
    /// </summary>
    /// <param name="source">The original text.</param>
    /// <param name="target">The target text produced by applying the patches to the <paramref name="source"/>.</param>
    /// <returns>Rope of <see cref="Patch{char}"/> objects.</returns>
    [Pure]
    public static Rope<Patch<char>> Create(string source, string target, PatchOptions? patchOptions = null, DiffOptions<char>? diffOptions = null) =>
        Create(source.ToRope(), target.ToRope(), patchOptions, diffOptions);

    /// <summary>
    /// Compute a list of patches to turn <paramref name="source"/> into <paramref name="target"/>.
    /// A set of diffs will be computed.
    /// </summary>
    /// <param name="source">The original list of items.</param>
    /// <param name="target">The target list of items produced by applying the patches to the <paramref name="source"/>.</param>
    /// <returns>Rope of <see cref="Patch{T}"/> objects.</returns>
    [Pure]
    public static Rope<Patch<T>> Create<T>(Rope<T> source, Rope<T> target, PatchOptions? patchOptions, DiffOptions<T>? diffOptions = null) where T : IEquatable<T>
    {
        // No diffs provided, compute our own.
        diffOptions = diffOptions ?? DiffOptions<T>.Default;
        using var deadline = diffOptions.StartTimer();
        var diffs = source.Diff(target, diffOptions, deadline.Cancellation);
        if (diffs.Count > 2)
        {
            diffs = diffs.DiffCleanupSemantic(deadline.Cancellation);
            diffs = diffs.DiffCleanupEfficiency(diffOptions);
        }

        return diffs.ToPatches(source, patchOptions);
    }
}
