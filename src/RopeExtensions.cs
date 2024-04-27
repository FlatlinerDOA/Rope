using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rope;

public static class RopeExtensions
{
    /// <summary>
    /// Maximum number of bytes before the GC basically chucks our buffers on the garbage heap.
    /// </summary>
    public const int LargeObjectHeapBytes = 85_000 - 24;

    internal static int CalculateAlignedBufferLength<T>(int cacheLineSize = 64)
    {
        var elementSize = Unsafe.SizeOf<T>();
        var numberOfElements = LargeObjectHeapBytes / elementSize;
        
        // Calculate the initial buffer size.
        var bufferSize = numberOfElements * elementSize;

        // Calculate the padding needed to make the buffer size a multiple of the cache line size.
        var padding = cacheLineSize - (bufferSize % cacheLineSize);

        // If the buffer size is already a multiple of the cache line size, padding will be equal to cacheLineSize. We don't need extra padding in that case.
        if (padding == cacheLineSize)
        {
            padding = 0;
        }

        // Return the aligned buffer size.
        var alignedBufferSize = bufferSize + padding;
        
        // Calculate the number of elements that can fit in the aligned buffer size.
        return alignedBufferSize / elementSize;
    }

    internal static int IntPow(this int x, uint pow)
	{
		int ret = 1;
		for (var p = 0; p < pow; p++)
		{
			ret *= x;
		}
	
		return ret;
	}

	/// <summary>
	/// Creates a new <see cref="Rope{char}"/> from the string provided.
	/// </summary>
	/// <typeparam name="T">The type of elements, must be <see cref="IEquatable{T}"/>.</typeparam>
	/// <param name="array">The array of items to construct from.</param>
	/// <returns>A new rope or <see cref="Rope{char}.Empty"/> if the array is empty.</returns>
	public static Rope<char> ToRope(this string text)
	{
		return new Rope<char>(text.AsMemory());
	}

	/// <summary>
	/// Creates a new rope from the array provided.
	/// </summary>
	/// <typeparam name="T">The type of elements, must be <see cref="IEquatable{T}"/>.</typeparam>
	/// <param name="array">The array of items to construct from.</param>
	/// <returns>A new rope or <see cref="Rope{T}.Empty"/> if the array is empty.</returns>
	public static Rope<T> ToRope<T>(this T[] array) where T : IEquatable<T>
	{
		return new Rope<T>(array.AsMemory());
	}

	/// <summary>
	/// Creates a new rope sequence from a sequence of elements.
	/// </summary>
	/// <typeparam name="T">The type of elements, must be <see cref="IEquatable{T}"/>.</typeparam>
	/// <param name="items">The sequence of items to construct from.</param>
	/// <returns>A new rope or <see cref="Rope{T}.Empty"/> if the sequence is empty.</returns>
	public static Rope<T> ToRope<T>(this IEnumerable<T> items) where T : IEquatable<T>
	{
		if (items is List<T> list)
		{
			ReadOnlySpan<T> span = CollectionsMarshal.AsSpan(list);
			return Rope<T>.FromReadOnlySpan(ref span);
		}

        if (items is T[] array)
        {
            return new Rope<T>(array);
        }

        if (items is IReadOnlyList<T> readOnlyList)
		{
			return Rope<T>.FromReadOnlyList(readOnlyList);
		}
		
		return Rope<T>.FromEnumerable(items);
	}

	/// <summary>
	/// Constructs a new Rope from a series of leaves into a tree.
	/// </summary>
	/// <param name="leaves">The leaf nodes to construct into a tree.</param>
	/// <returns>A new rope with the leaves specified.</returns>
	public static Rope<T> Combine<T>(this Rope<Rope<T>> leaves) where T : IEquatable<T>
	{
		return Rope<T>.Combine(leaves);
	}

    /// <summary>
    /// Constructs a new Rope from a series of leaves into a tree.
    /// </summary>
    /// <param name="leaves">The leaf nodes to construct into a tree.</param>
    /// <returns>A new rope with the leaves specified.</returns>
    public static Rope<T> Combine<T>(this IEnumerable<Rope<T>> leaves) where T : IEquatable<T>
    {
        return Rope<T>.Combine(leaves.ToRope());
    }

    /// <summary>
    /// Determine if the suffix of one string is the prefix of another.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="text1">First string.</param>
    /// <param name="text2"> Second string.</param>
    /// <returns>The number of characters common to the end of the first
    /// string and the start of the second string.</returns>
    [Pure]
    public static int CommonOverlapLength<T>(this Rope<T> text1, Rope<T> text2) where T : IEquatable<T>
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
}