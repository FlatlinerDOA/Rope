using System.Runtime.InteropServices;

namespace Rope;

public static class RopeExtensions
{
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
}