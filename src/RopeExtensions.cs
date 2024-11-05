using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Rope.Compare;

namespace Rope;

public static class RopeExtensions
{
    /// <summary>
    /// Maximum number of bytes before the GC basically chucks our buffers on the garbage heap.
    /// </summary>
    public const int LargeObjectHeapBytes = 85_000 - 24;

    /// <summary>
    /// An attempt to determine a CPU-cache aligned buffer size for the given input type.
    /// </summary>
    /// <typeparam name="T">The element being sized.</typeparam>
    /// <param name="cacheLineSize">The default CPU cache line to optimise for.</param>
    /// <returns>An integer of the number of elements that makes for a CPU aligned buffer.</returns>
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

    /// <summary>
    /// Math.Pow for integers
    /// </summary>
    /// <param name="x">The integer to multiply to the power of</param>
    /// <param name="pow">The power to multiply</param>
    /// <returns>A power of the input integer.</returns>
    internal static int IntPow(this int x, uint pow)
    {
        int ret = 1;
        for (var p = 0; p < pow; p++)
        {
            ret *= x;
        }

        return ret;
    }

    public static LeasedSpan<T> Lease<T>(this ArrayPool<T> source, int length) => new LeasedSpan<T>(source, length);

    public ref struct LeasedSpan<T>
    {
        private ArrayPool<T> pool;
        private T[] rented;
        private Span<T> span;

        public LeasedSpan(ArrayPool<T> pool, int length)
        {
            this.pool = pool;
            this.rented = pool.Rent(length);  // Rent it.
            this.span = this.rented.AsSpan().Slice(0, length); // Cut it down to size.
        }

        public Span<T> Span => this.span;

        public void Dispose()
        {
            this.pool.Return(this.rented, true); // Always return it clean!
        }
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
        return items switch
        {
            Rope<T> rope => rope,
            List<T> list => Rope<T>.FromList(list),
            T[] array => new Rope<T>(array.AsMemory()),
            IReadOnlyList<T> readOnlyList => Rope<T>.FromReadOnlyList(readOnlyList),
            _ => Rope<T>.FromEnumerable(items)
        };
    }

    /// <summary>
    /// Constructs a new Rope from a series of leaves into a tree (flattens a sequence of sequences).
    /// </summary>
    /// <param name="leaves">The leaf nodes to construct into a tree.</param>
    /// <returns>A new rope with the leaves specified.</returns>
    public static Rope<T> Combine<T>(this Rope<Rope<T>> leaves) where T : IEquatable<T>
    {
        return Rope<T>.Combine(leaves);
    }

    /// <summary>
    /// Constructs a new Rope from a series of leaves into a tree (flattens a sequence of sequences).
    /// </summary>
    /// <param name="leaves">The leaf nodes to construct into a tree.</param>
    /// <returns>A new rope with the leaves specified.</returns>
    public static Rope<T> Combine<T>(this IEnumerable<Rope<T>> leaves) where T : IEquatable<T>
    {
        return Rope<T>.Combine(leaves.ToRope());
    }

    /// <summary>
    /// Replaces all occurrences of a specified string within the source <see cref="Rope{char}"/> with another string.
    /// </summary>
    /// <param name="source">The source <see cref="Rope{char}"/> in which to perform the replacement.</param>
    /// <param name="replace">The <see cref="Rope{char}"/> to be replaced.</param>
    /// <param name="with">The <see cref="Rope{char}"/> to replace all occurrences of <paramref name="replace"/>.</param>
    /// <param name="comparison">The string comparison rule to use when matching.</param>
    /// <returns>
    /// A new <see cref="Rope{char}"/> that is equivalent to the source <see cref="Rope{char}"/> except that all instances of <paramref name="replace"/> 
    /// are replaced with <paramref name="with"/>. If <paramref name="replace"/> is not found in the source <see cref="Rope{char}"/>, 
    /// the method returns the original <see cref="Rope{char}"/>.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when an unsupported <paramref name="comparison"/> option is provided.
    /// </exception>
    /// <remarks>
    /// This method uses different character comparers based on the specified <paramref name="comparison"/> option:
    /// - For Ordinal comparison, it uses the default Replace method.
    /// - For OrdinalIgnoreCase, it uses a custom OrdinalIgnoreCaseCharComparer.
    /// - For culture-specific comparisons (CurrentCulture, CurrentCultureIgnoreCase, InvariantCulture, InvariantCultureIgnoreCase),
    ///   it uses the appropriate <see cref="CharComparer"/>.
    /// The method is case-sensitive or case-insensitive based on the chosen comparison option.
    /// </remarks>
    /// <example>
    /// <code>
    /// Rope<char> source = new Rope<char>("Hello, World!");
    /// Rope<char> result = source.Replace(new Rope<char>("o"), new Rope<char>("0"), StringComparison.OrdinalIgnoreCase);
    /// // result will be "Hell0, W0rld!"
    /// </code>
    /// </example>
    public static Rope<char> Replace(this Rope<char> source, Rope<char> replace, Rope<char> with, StringComparison comparison) =>
        comparison switch
        {
            StringComparison.Ordinal => source.Replace(replace, with),
            StringComparison.OrdinalIgnoreCase => source.Replace(replace, with, CharComparer.OrdinalIgnoreCase),
            StringComparison.CurrentCulture => source.Replace(replace, with, CharComparer.CurrentCulture),
            StringComparison.CurrentCultureIgnoreCase => source.Replace(replace, with, CharComparer.CurrentCultureIgnoreCase),
            StringComparison.InvariantCulture => source.Replace(replace, with, CharComparer.InvariantCulture),
            StringComparison.InvariantCultureIgnoreCase => source.Replace(replace, with, CharComparer.InvariantCultureIgnoreCase),
            _ => throw new NotSupportedException(),
        };

    /// <summary>
    /// Joins a sequence of elements with a nominated separator.
    /// </summary>
    /// <typeparam name="T">The item to separate each element.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="separator">The separating item.</param>
    /// <returns>A sequence with a separator interleaved.</returns>
    public static Rope<T> Join<T>(this IEnumerable<Rope<T>> source, T separator) where T : IEquatable<T>
    {
        var result = Rope<T>.Empty;
        bool separate = false;
        foreach (var item in source)
        {
            if (separate)
            {
                result += separator;
            }
            else
            {
                separate = true;
            }

            result += item;
        }

        return result;
    }

    /// <summary>
    /// Joins a sequence of elements with a nominated separator.
    /// </summary>
    /// <typeparam name="T">The item to separate each element.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="separator">The separating item.</param>
    /// <returns>A sequence with a separator interleaved.</returns>
    public static Rope<T> Join<T>(this IEnumerable<Rope<T>> source, Rope<T> separator) where T : IEquatable<T>
    {
        var result = Rope<T>.Empty;
        bool separate = false;
        foreach (var item in source)
        {
            if (separate)
            {
                result += separator;
            }
            else
            {
                separate = true;
            }

            result += item;
        }

        return result;
    }
}