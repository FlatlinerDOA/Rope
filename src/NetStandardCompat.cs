// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETSTANDARD2_1

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}


namespace System.Runtime.InteropServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class CollectionsMarshal 
    {    
        public static Span<T> AsSpan<T>(this List<T> list) => list.ToArray();
    }
}

namespace System.Linq
{
    public static class EnumerableCompat
    {
        public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
        {
            return ChunkIterator(source, size);
        }

        private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
        {
            using IEnumerator<TSource> e = source.GetEnumerator();

            // Before allocating anything, make sure there's at least one element.
            if (e.MoveNext())
            {
                // Now that we know we have at least one item, allocate an initial storage array. This is not
                // the array we'll yield.  It starts out small in order to avoid significantly overallocating
                // when the source has many fewer elements than the chunk size.
                int arraySize = Math.Min(size, 4);
                int i;
                do
                {
                    var array = new TSource[arraySize];

                    // Store the first item.
                    array[0] = e.Current;
                    i = 1;

                    if (size != array.Length)
                    {
                        // This is the first chunk. As we fill the array, grow it as needed.
                        for (; i < size && e.MoveNext(); i++)
                        {
                            if (i >= array.Length)
                            {
                                arraySize = (int)Math.Min((uint)size, 2 * (uint)array.Length);
                                Array.Resize(ref array, arraySize);
                            }

                            array[i] = e.Current;
                        }
                    }
                    else
                    {
                        // For all but the first chunk, the array will already be correctly sized.
                        // We can just store into it until either it's full or MoveNext returns false.
                        TSource[] local = array; // avoid bounds checks by using cached local (`array` is lifted to iterator object as a field)
                        for (; (uint)i < (uint)local.Length && e.MoveNext(); i++)
                        {
                            local[i] = e.Current;
                        }
                    }

                    if (i != array.Length)
                    {
                        Array.Resize(ref array, i);
                    }

                    yield return array;
                }
                while (i >= size && e.MoveNext());
            }
        }
    }
}

namespace System
{
    internal static class MemoryExtensionsCompat
    {
        public static bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
        {
            if (span.Length != other.Length)
            {
                return false;
            }

            // Use the comparer to compare each element.
            comparer ??= EqualityComparer<T>.Default;
            for (int i = 0; i < span.Length; i++)
            {
                if (!comparer.Equals(span[i], other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static int CommonPrefixLength<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
        {
            SliceLongerSpanToMatchShorterLength(ref span, ref other);

            // Ensure we have a comparer, then compare the spans.
            comparer ??= EqualityComparer<T>.Default;
            for (int i = 0; i < span.Length; i++)
            {
                if (!comparer.Equals(span[i], other[i]))
                {
                    return i;
                }
            }

            return span.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SliceLongerSpanToMatchShorterLength<T>(ref ReadOnlySpan<T> span, ref ReadOnlySpan<T> other)
        {
            if (other.Length > span.Length)
            {
                other = other.Slice(0, span.Length);
            }
            else if (span.Length > other.Length)
            {
                span = span.Slice(0, other.Length);
            }
            Debug.Assert(span.Length == other.Length);
        }
    }
}
#endif