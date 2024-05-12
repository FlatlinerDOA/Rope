using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rope.UnitTests;

internal class TestData
{
    internal static readonly Rope<int> EvenNumbers = Enumerable.Range(0, 2048).Where(i => i % 2 == 0).ToRope();

    internal static readonly Rope<char> LargeText = Enumerable.Range(0, 32 * 1024).Select(i => (char)(65 + (i % 26))).ToRope();

    internal static (string, Rope<char>) Create(int length, int chunkSize)
    {
        if (length == 0)
        {
            return (string.Empty, Rope<char>.Empty);
        }

        var chars = Enumerable.Range(0, length).Select(i => (char)(65 + (i % 26))).ToArray();
        var expected = new string(chars);
        var rope = chars.Chunk(chunkSize).Select(t => t.ToRope()).Combine();
        return (expected, rope);
    }
}
