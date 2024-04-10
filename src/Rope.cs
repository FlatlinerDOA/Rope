﻿// Copyright 2024 Andrew Chisholm (https://github.com/FlatlinerDOA)

namespace Rope;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Diagnostics.Metrics;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;

/// <summary>
/// A rope is an immutable sequence built using a b-tree style data structure that is useful for efficiently applying and storing edits, most commonly to text, but any list or sequence can be edited.
/// </summary>
public sealed class Rope<T> : IEnumerable<T>, IReadOnlyList<T>, IImmutableList<T>, IEquatable<Rope<T>> where T : IEquatable<T>
{
	/// <summary>
	/// Maximum tree depth allowed.
	/// </summary>
	public const int MaxTreeDepth = 46;

	/// <summary>
	/// Maximum number of bytes before the GC basically chucks our buffers on the garbage heap.
	/// </summary>
	public const int LargeObjectHeapBytes = 85_000 - 24;

	/// <summary>
	/// Defines the maximum depth descrepancy between left and right to cause a re-split of one side when balancing.
	/// </summary>
	public const int MaxDepthImbalance = 4;

  	private static readonly Meter meter = new Meter("rope");

    private static readonly Counter<int> rebalances = meter.CreateCounter<int>($"rope.{typeof(T).Name}.rebalances");

	/// <summary>
	/// Static size of the maximum length of a leaf node in the rope's binary tree. This is used for balancing the tree.
	/// This is calculated to never require Large Object Heap allocations.
	/// </summary>
	public static readonly int MaxLeafLength = LargeObjectHeapBytes / Unsafe.SizeOf<T>();

    /// <summary>
    /// Gets the maximum number of elements any rope can have. 
	/// For <see cref="byte"/> this is 5,979,654,405,241,176,064 bytes (5,311 petabytes).
	/// For <see cref="char"/> this is 2,989,827,202,620,588,032 characters.
	/// For <see cref="int"/> this is 1,494,913,601,310,294,016 ints.
    /// </summary>
    public static readonly long MaxLength = (long)Math.Pow(2, MaxTreeDepth) * (long)MaxLeafLength;

	/// <summary>
	/// Defines the Empty leaf.
	/// </summary>
	public static readonly Rope<T> Empty = new Rope<T>();

	private static readonly double phi = (1 + Math.Sqrt(5)) / 2;

	private static readonly double phiDiv = (2 * phi - 1);

	/// <summary>
	/// Defines the minimum lengths the leaves should be in relation to the depth of the tree.
	/// </summary>
	private static readonly int[] DepthToFibonnaciPlusTwo = Enumerable.Range(0, MaxTreeDepth).Select(d => Fibonnaci(d) + 2).ToArray();	

	/// <summary>Left slice of the raw buffer</summary>
	private readonly Rope<T>? left;

	/// <summary>Right slice of the raw buffer (null if a leaf node.)</summary>
	private readonly Rope<T>? right;
	
	/// <summary>Data is the raw underlying buffer, or a cached copy of the ToMemory call (for efficiency).</summary>
	private readonly ReadOnlyMemory<T> data;

	/// <summary>
	/// Creates a new instance of Rope{T}. Use Empty instead.
	/// </summary>
	private Rope()
	{
		// Empty rope is just a leaf node.
		this.data = ReadOnlyMemory<T>.Empty;
		this.Depth = 0;
		this.IsBalanced = true;
	}

	/// <summary>
	/// Creates a new instance of Rope{T}.
	/// </summary>
	/// <param name="data">The data to wrap in a leaf node.</param>
	public Rope(ReadOnlyMemory<T> data)
	{
		// NOTE: Previously we would split the rope immediately, it has been determined through benchmarking that
		// this is only really necessary when performing edits. In most cases it is best to just wrap the memory
		// and then decide how to split that memory, based on the edit required.
		
		// Always initialize a leaf node when given memory directly.
		this.data = data;
		this.Depth = 0;
		this.IsBalanced = true;
	}

	/// <summary>
	/// Creates a new instance of Rope{T}.
	/// </summary>
	/// <param name="left">The left child node.</param>
	/// <param name="right">The right child node.</param>
	/// <exception cref="ArgumentNullException">Thrown if either the left or right node is null.</exception>
	public Rope(Rope<T> left, Rope<T> right)
	{
		if (left == null)
		{
			throw new ArgumentNullException(nameof(left));
		}

		if (right == null)
		{
			throw new ArgumentNullException(nameof(right));
		}
		
		if (right.Length == 0)
		{
            if (left.IsNode)
            {
                this.left = left.left;
                this.right = left.right;
                this.Depth = Math.Max(this.left.Depth, this.right.Depth) + 1;
                this.IsBalanced = this.CalculateIsBalanced();

            }
            else
            {
                this.data = left.data;
                this.Depth = 0;
				this.IsBalanced = true;
            }
        }
		else if (left.Length == 0)
		{
            if (right.IsNode)
            {
                this.left = right.left;
                this.right = right.right;
                this.Depth = Math.Max(this.left.Depth, this.right.Depth) + 1;
                this.IsBalanced = this.CalculateIsBalanced();
            }
            else
            {
                this.data = right.data;
                this.Depth = 0;
				this.IsBalanced = true;
            }
        }
		else
		{
			this.data = ReadOnlyMemory<T>.Empty;
			this.left = left;
			this.right = right;
			this.Depth = Math.Max(left.Depth, right.Depth) + 1;
            this.IsBalanced = this.CalculateIsBalanced();

            // if (newDepth > MaxTreeDepth)
            // {
            // 	// This is the only point at which we are actually increasing the depth beyond the limit, this is where a re-balance may be necessary.
            // 	this.left = left.Balanced();
            // 	this.right = right.Balanced();
            // 	this.Depth =  Math.Max(this.left.Depth, this.right.Depth) + 1;
            // }
        }
	}

    public T this[long index] => this.ElementAt(index);

	/// <summary>
	/// Gets a range of elements in the form of a new instance of <see cref="Rope{T}"/>.
	/// </summary>
	/// <param name="range">The range to select.</param>
	/// <returns>A new rope instance if the slice does not cover the entire sequence, otherwise returns the original instance.</returns>
	public Rope<T> this[Range range]
	{
		get
		{
			var (offset, length) = range.GetOffsetAndLength((int)this.Length);
			return this.Slice(offset, length);
		}
	}

	/// <summary>
	/// Gets the left or prefix branch of the rope. May be null if this is a leaf node.
	/// </summary>
	public Rope<T>? Left => this.left;

	/// <summary>
	/// Gets the right or suffix branch of the rope. May be null if this is a leaf node.
	/// </summary>
	public Rope<T>? Right => this.right;

	/// <summary>
	/// Gets a value indicating whether this is a Node and Left and Right will be non-null, 
	/// otherwise it is a leaf and just wraps a slice of read only memory.
	/// </summary>
	[MemberNotNullWhen(true, nameof(this.left))]
	[MemberNotNullWhen(true, nameof(this.right))]
	[MemberNotNullWhen(true, nameof(this.Left))]
	[MemberNotNullWhen(true, nameof(this.Right))]
	public bool IsNode => this.Depth > 0;

	/// <summary>
	/// Gets the length of the left Node if this is a node (the split-point essentially), otherwise the length of the data. 
	/// </summary>
	public long Weight => this.left?.Length ?? this.data.Length;

	/// <summary>
	/// Gets the length of the rope in terms of the number of elements it contains.
	/// </summary>
	public long Length => this.IsNode ? this.left.Length + this.right.Length : this.data.Length;

	/// <summary>
	/// Gets a value indicating whether this rope is empty.
	/// </summary>
	public bool IsEmpty => this.Length == 0;

	/// <summary>
	/// Gets the maximum depth of the tree, returns 0 if this is a leaf, never exceeds <see cref="Rope{T}.MaxTreeDepth"/>.
	/// </summary>
	public int Depth { get; }

	/// <summary>
	/// Gets the element at the specified index.
	/// </summary>
	/// <param name="index"></param>
	/// <returns>The element at the specified index.</returns>
	/// <exception cref="IndexOutOfRangeException">Thrown if index is larger than or equal to the length or less than 0.</exception>
	[Pure]
	public T ElementAt(long index)
	{
		if (this.IsNode)
		{
			if (this.Weight <= index && this.right.Length != 0)
			{
				return this.right.ElementAt(index - this.Weight);
			}

			if (this.left.Length != 0)
			{
				return this.left.ElementAt(index);
			}

			throw new IndexOutOfRangeException(nameof(index));
		}

		return this.data.Span[(int)index];
	}

	/// <summary>
	/// Gets an enumerable of slices of this rope, splitting by the given separator element.
	/// </summary>
	/// <param name="separator">The element to separate by, this element will never be included in the returned sequence.</param>
	/// <returns>Zero or more ropes splitting the rope by it's separator.</returns>
	[Pure]
	public IEnumerable<Rope<T>> Split(T separator)
	{
		Rope<T> remainder = this;
		do
		{
			var i = remainder.IndexOf(separator);
			if (i != -1)
			{
				yield return remainder.Slice(0, i);
				remainder = remainder.Slice(i + 1);
			}
			else
			{
				yield return remainder;
				yield break;
			}
		}
		while (true);
	}

	/// <summary>
	/// Gets an enumerable of slices of this rope, splitting by the given separator sequence.
	/// </summary>
	/// <param name="separator">The sequence of elements to separate by, this sequenece will never be included in the returned sequence.</param>
	/// <returns>Zero or more ropes splitting the rope by it's separator.</returns>
	[Pure]
	public IEnumerable<Rope<T>> Split(ReadOnlyMemory<T> separator)
	{
		Rope<T> remainder = this;
		do
		{
			var i = remainder.IndexOf(separator);

			if (i != -1)
			{
				yield return remainder.Slice(0, i);
				remainder = remainder.Slice(i + separator.Length);
			}
			else
			{
				yield return remainder;
				yield break;
			}
		}
		while (true);
	}

	/// <summary>
	/// Splits a rope in two to pieces a left and a right half at the specified index.
	/// </summary>
	/// <param name="i">The index to split the two halves at.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if index to split at is out of range.</exception>
	/// <returns></returns>
	[Pure]
	public (Rope<T> Left, Rope<T> Right) SplitAt(long i)
	{
		if (this.IsNode)
		{
			if (i == 0)
			{
				return (Empty, this);
			}
			else if (i == this.Length)
			{
				return (this, Empty);
			}
			else if (i <= this.Weight)
			{
				var (newLeft, newRight) = this.left.SplitAt(i);
				return (newLeft, new Rope<T>(newRight, this.right));
			}
			else
			{
				var (a, b) = this.right.SplitAt(i - this.Weight);
				return (new Rope<T>(this.left, a), b);
			}
		}

		return (new Rope<T>(this.data.Slice(0, (int)i)), new Rope<T>(this.data.Slice((int)i)));
	}

	/// <summary>
	/// Replaces a single element at the given index.
	/// </summary>
	/// <param name="index">The index to insert at.</param>
	/// <param name="item">The item to replace with at the given index.</param>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the item included as a replacement.</returns>
	[Pure]
	public Rope<T> SetItem(long index, T item)
	{
		var (left, right) = this.SplitAt(index);		
		return new Rope<T>(left, new Rope<T>(new[] { item }, right.Slice(1))).Balanced();
	}


	/// <summary>
	/// Inserts a single element at the given index.
	/// </summary>
	/// <param name="index">The index to insert at.</param>
	/// <param name="items">The items to insert at the given index.</param>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the items added.</returns>
	[Pure]
	public Rope<T> Insert(long index, T item)
	{
		return this.InsertRange(index, new Rope<T>(new[] { item }.AsMemory()));
	}

	/// <summary>
	/// Inserts a sequence of elements at the given index.
	/// </summary>
	/// <param name="index">The index to insert at.</param>
	/// <param name="items">The items to insert at the given index.</param>
	/// <returns>A new instance of the full <see cref="Rope{T}"/> with the the items added at the specified index.</returns>
	[Pure]
	public Rope<T> InsertRange(long index, ReadOnlyMemory<T> items)
	{
		return this.InsertRange(index, new Rope<T>(items));
	}

	/// <summary>
	/// Inserts a sequence of elements at the given index.
	/// </summary>
	/// <param name="index">The index to insert at.</param>
	/// <param name="items">The items to insert at the given index.</param>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the items added.</returns>
	[Pure]
	public Rope<T> InsertRange(long index, Rope<T> items)
	{
		if (index > this.Length)
		{
			throw new IndexOutOfRangeException(nameof(index));
		}

		var (left, right) = this.SplitAt(index);
		return new Rope<T>(left, new Rope<T>(items, right));
	}

	/// <summary>
	/// Removes a subset of elements for a given range.
	/// </summary>
	/// <param name="start">The start index to remove from.</param>
	/// <param name="length">The number of items to be removed.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown if start is greater than Length, or start + length exceeds the Length.</exception>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the items removed if length is non-zero. Otherwise returns the original instance.</returns>
	[Pure]
	public Rope<T> RemoveRange(long start, long length)
	{
		if (length == 0)
		{
			return this;
		}

		if (start == 0 && length == this.Length)
		{
			return Empty;
		}

		return new Rope<T>(this.Slice(0, start), this.Slice(start + length));
	}

	/// <summary>
	/// Removes the tail range of elements from a given starting index (Alias for Slice).
	/// </summary>
	/// <param name="start">The start index to remove from.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown if start is greater than Length</exception>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the items removed if start is non-zero. Otherwise returns the original instance.</returns>
	[Pure]
	public Rope<T> RemoveRange(long start)
	{
		if (start == this.Length) 
		{
			return this;
		}

		return this.Slice(start, this.Length - start);	
	}

	/// <summary>
	/// Removes the tail range of elements from a given starting index (Alias for Slice).
	/// </summary>
	/// <param name="start">The start index to remove from.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown if start is greater than Length</exception>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the items removed if start is non-zero. Otherwise returns the original instance.</returns>
	[Pure]
	public Rope<T> RemoveAt(long index)
	{
		if (index == this.Length) 
		{
			return this;
		}

		var (left, right) = this.SplitAt(index);
		return new Rope<T>(left, right.Slice(1));
	}

	/// <summary>
	/// Adds an item to the end of the rope.
	/// Performance note: This may attempt to perform a balancing.
	/// </summary>
	/// <param name="items">The item to append to this rope.</param>
	/// <returns>A new rope that is the concatenation of the current sequence and the specified item.</returns>
	[Pure]
	public Rope<T> Add(T item) => this.AddRange(new[] { item });

	/// <summary>
	/// Concatenates two sequences together into a single sequence.
	/// Performance note: This may attempt to perform a balancing.
	/// </summary>
	/// <param name="items">The items to append to this rope.</param>
	/// <returns>A new rope that is the concatenation of the current sequence and the specified items.</returns>
	[Pure]
	public Rope<T> AddRange(Rope<T> items)
	{
		if (items.Length == 0)
		{
			return this;
		}

		if (this.Length == 0)
		{
			return items;
		}

		return new Rope<T>(this, items).Balanced();
	}

	/// <summary>
	/// Gets a range of elements in the form of a new instance of <see cref="Rope{T}"/>.
	/// </summary>
	/// <param name="start">The start to select from.</param>
	/// <returns>A new rope instance if start is non-zero, otherwise returns the original instance.</returns>
	[Pure]
	public Rope<T> Slice(long start)
	{
		return this.Slice(start, this.Length - start);
	}

	/// <summary>
	/// Gets a range of elements in the form of a new instance of <see cref="Rope{T}"/>.
	/// </summary>
	/// <param name="start">The start to select from.</param>
	/// <param name="length">The number of elements to return.</param>
	/// <returns>A new rope instance if the slice does not cover the entire sequence, otherwise returns the original instance.</returns>
	[Pure]
	public Rope<T> Slice(long start, long length)
	{
		if (start == 0 && length == this.Length)
		{
			return this;
		}

		var (head, _) = this.SplitAt(start + length);
		return head.SplitAt(start).Right;
	}

	[Pure]
	public static bool operator ==(Rope<T> a, Rope<T> b) => Rope<T>.Equals(a, b);

	[Pure]
	public static bool operator !=(Rope<T> a, Rope<T> b) => !Rope<T>.Equals(a, b);

	/// <summary>
	/// Concatenates two rope instances together into a single sequence.
	/// </summary>
	/// <param name="a">The first sequence.</param>
	/// <param name="b">The second sequence.</param>
	/// <returns>A new rope instance concatenating the two sequences.</returns>
	[Pure]
	public static Rope<T> operator +(Rope<T> a, Rope<T> b) => a.AddRange(b);

	/// <summary>
	/// Appends an element to the existing instance.
	/// </summary>
	/// <param name="a">The first sequence.</param>
	/// <param name="b">The second element to append to the sequence.</param>
	/// <returns>A new rope instance concatenating the two sequences.</returns>
	[Pure]
	public static Rope<T> operator +(Rope<T> a, T b) => a.Add(b);

	/// <summary>
	/// Implicitly converts a read only memory sequence into a rope.
	/// </summary>
	/// <param name="a">A section of memory to wrap in a rope instance.</param>
	[Pure]
	public static implicit operator Rope<T> (ReadOnlyMemory<T> a) => new Rope<T>(a);
	
	[Pure]
	public static implicit operator Rope<T> (T[] a) => new Rope<T>(a);

	/// <summary>
	/// Determines if the rope's binary tree is unbalanced and then recursively rebalances if necessary.
	/// </summary>
	/// <returns>A balanced tree or the original rope if not out of range.</returns>
	[Pure]
	public Rope<T> Balanced()
	{
		// Early return if the tree is already balanced or not a node		
    	if (!this.IsNode || this.IsBalanced)
		{
			return this;
		}

		rebalances.Add(1);

		if (this.Length <= MaxLeafLength)
		{
			// If short enough brute force rebalance into a single leaf.
			return new Rope<T>(this.ToMemory());
		}

		// Calculate the depth difference between left and right
		var leftDepth = this.Left.Depth;
		var rightDepth = this.Right.Depth;
		var depthDiff = rightDepth - leftDepth;
		
		////Debug.Assert(depthDiff <= MaxTreeDepth, "This tree is way too deep?");
		if (depthDiff > MaxDepthImbalance)
		{
			// Example: Right is deep (10), left is shallower (6) -> +4 imbalance
			var (newLeftPart, newRightPart) = this.Right.SplitAt(this.Right.Length / 2);
        	return new Rope<T>(new Rope<T>(this.Left, newLeftPart).Balanced(), newRightPart.Balanced());
		}
		else if (depthDiff < -MaxDepthImbalance)
		{
			// Example: Left is deep (10), Right is shallower (6) -> -4 imbalance
			var (newLeftPart, newRightPart) = this.Left.SplitAt(this.Left.Length / 2);
        	return new Rope<T>(newLeftPart.Balanced(), new Rope<T>(newRightPart, this.Right).Balanced());
		}

		// Recursively balance if we are already very long.
        var (left, right) = this.SplitAt(this.Length / 2);
        return new Rope<T>(left.Balanced(), right.Balanced());
	}

	/// <summary>
	/// Determines if this rope's binary tree is unbalanced.
	/// </summary>
	/// <remarks>
	/// https://www.cs.rit.edu/usr/local/pub/jeh/courses/QUARTERS/FP/Labs/CedarRope/rope-paper.pdf
	/// | p. 1319 - We define the depth of a leaf to be 0, and the depth of a concatenation to be
	/// | one plus the maximum depth of its children. Let Fn be the nth Fibonacci number.
	/// | A rope of depth n is balanced if its length is at least Fn+2, e.g. a balanced rope
	/// | of depth 1 must have length at least 2.
	//// </remarks>
	public bool IsBalanced { get; }

    public int Count => (int)this.Length;

    public T this[int index] => this[(long)index];

    private static int Fibonnaci(int n)
	{
		n = Math.Min(n, MaxTreeDepth);

		// Binet's Formula - https://en.wikipedia.org/wiki/Fibonacci_number#Closed-form_expression
		return (int)((Math.Pow(phi, n) - Math.Pow(-phi, -n)) / phiDiv);
	}
	
	/// <summary>
	/// Calculates the longest common prefix length (The number of elements that are shared at the start of the sequence) between this sequence and another sequence.
	/// </summary>
	/// <param name="other">The other sequence to compare shared prefix length.</param>
	/// <returns>A number greater than or equal to the length of the shortest of the two sequences.</returns>
	[Pure]
	public long CommonPrefixLength(Rope<T> other)
	{
		if (!this.IsNode && !other.IsNode)
		{
			return MemoryExtensions.CommonPrefixLength(this.data.Span, other.data.Span);
		}

		// TODO: Compare Performance
		////return MemoryExtensions.CommonPrefixLength(this.ToMemory().Span, other.ToMemory().Span);

		// Performance analysis: https://neil.fraser.name/news/2007/10/09/
		var n = Math.Min(this.Length, other.Length);
		for (long i = 0; i < n; i++)
		{
			if (!this.ElementAt(i).Equals(other.ElementAt(i)))
			{
				return i;
			}
		}

		return n;
	}

	/// <summary>
	/// Determine the common suffix length of two sequences (The number of elements that are shared at the end of the sequence) between this sequence and another sequence.
	/// </summary>
	/// <param name="other">Other sequence to compare against.</param>
	/// <returns>The number of characters common to the end of each sequence.</returns>
	[Pure]
	public long CommonSuffixLength(Rope<char> other)
	{
		// Performance analysis: https://neil.fraser.name/news/2007/10/09/
		var n = Math.Min(this.Length, other.Length);
		for (var i = 1; i <= n; i++)
		{
			if (!this.ElementAt(this.Length - i).Equals(other.ElementAt(other.Length - i)))
			{
				return i - 1;
			}
		}

		return n;
	}

	/// <summary>
	/// Copies the rope into a new single contiguous array of elements.
	/// </summary>
	/// <returns>A new array filled with the contents of the sequence.</returns>
	[Pure]
	public T[] ToArray()
	{
		if (this.IsNode)
		{
			// Instead of: new T[this.left.Length + this.right.Length]; we use an uninitalized array as we are copying over the entire contents.
			var result = GC.AllocateUninitializedArray<T>((int)(this.left.Length + this.right.Length));
			var mem = result.AsMemory();
			this.left.CopyTo(mem);
			this.right.CopyTo(mem.Slice((int)this.left.Length));
			return result;
		}

		return this.data.ToArray();
	}
	
	/// <summary>
	/// Copies the rope into the specified memory buffer.
	/// </summary>
	/// <param name="other">The target to copy to.</param>
	public void CopyTo(Memory<T> other)
	{
		if (this.IsNode)
		{
			// Binary tree so copy each half.
			this.left.CopyTo(other);
			this.right.CopyTo(other.Slice((int)this.left.Length));
		}
		else
		{
			// Leaf node so copy memory.
			this.data.CopyTo(other);
		}
	}
		
	[Pure]
	public long IndexOf(Rope<T> find) => this.IndexOf(find, EqualityComparer<T>.Default);

	/// <summary>
	/// Finds the index of a subset of the rope
	/// </summary>
	/// <param name="find"></param>
	/// <returns>A number greater than or equal to zero if the sub-sequence is found, otherwise returns -1.</returns>
	[Pure]
	public long IndexOf<TEqualityComparer>(Rope<T> find, TEqualityComparer comparer) where TEqualityComparer : IEqualityComparer<T>
	{
		if (find.Length > this.Length)
		{
			return -1;
		}

		if (find.Length == 0)
		{
			return 0;
		}

		if (!this.IsNode)
		{
			var dataSpan = this.data.Span;
			if (!find.IsNode && object.ReferenceEquals(EqualityComparer<T>.Default, comparer))
			{
				return dataSpan.IndexOf(find.data.Span);
			}

			// Check in the 'data' array for a starting match that could spill over to 'right'
			for (var i = 0; i < dataSpan.Length; i++)
			{
				bool match = true;
				for (var j = 0; j < find.Length && match; j++)
				{
					if (i + j < dataSpan.Length)
					{
						match = comparer.Equals(dataSpan[(int)(i + j)], find[j]);
					}
					else
					{
						match = false;
					}
				}

				if (match)
				{
					return i;
				}
			}

			return -1;
		}

		for (var i = 0; i < this.Length; i++)
		{
			bool match = true;
			for (var j = 0; j < find.Length && match; j++)
			{
				if (i + j < this.Length)
				{
					match = comparer.Equals(this[(int)(i + j)], find[j]);
				}
				else
				{
					match = false;
				}
			}

			if (match)
			{
				return i;
			}
		}

		// Indicate that no match was found
		return -1;
	}

	[Pure]
	public long IndexOf(Rope<T> find, long offset)
	{
		var i = this.Slice(offset).IndexOf(find);
		if (i != -1)
		{
			return i + offset;			
		}

		return -1;
	}

	[Pure]
	public long IndexOf(ReadOnlyMemory<T> find) 
	{
		if (this.IsNode)
		{
			// Use the complicated logic.
			return this.IndexOf(new Rope<T>(find));
		}
		else
		{
			// Leaf is quick and easy.
			return this.data.Span.IndexOf(find.Span);
		}
	}

	[Pure]
	public long IndexOf(T find)
	{
		if (this.IsNode)
		{
			// Node
			var i = this.left.IndexOf(find);
			if (i != -1)
			{
				return i;
			}

			i = this.right.IndexOf(find);
			if (i != -1)
			{
				return this.left.Length + i;
			}
		}
		else
		{
			// Leaf
			return this.data.Span.IndexOf(find);
		}

		return -1;
	}

	[Pure]
	public long IndexOf(T find, long offset)
	{
		var i = this.Slice(offset).IndexOf(find);
		if (i != -1)
		{
			return i + offset;
		}
		
		return -1;
	}

	[Pure]
	public long IndexOf(ReadOnlyMemory<T> find, long offset)
	{
		var i = this.Slice(offset).IndexOf(find);
		if (i != -1)
		{
			return i + offset;
		}

		return -1;
	}

	[Pure]
	public bool StartsWith(Rope<T> find) => this.IndexOf(find) == 0;

	[Pure]
	public bool StartsWith(ReadOnlyMemory<T> find) => this.StartsWith(new Rope<T>(find));


	public long LastIndexOf(Rope<T> find) => this.LastIndexOf(find, this.Length);
	
	/// <summary>
	/// Returns the last element index that matches the specified sub-sequence, working backwards from the startIndex (inclusive).
	/// </summary>
	/// <param name="find">The sequence to find, if empty will return the startIndex + 1.</param>
	/// <param name="startIndex">The starting index to start searching backwards from (Optional).</param>
	/// <returns>The last element index that matches the sub-sequence, skipping the offset elements.</returns>
	[Pure]
	public long LastIndexOf(Rope<T> find, long startIndex)
	{
		if (find.Length == 0)
		{
			// Adjust the return value to conform with .NET's behavior.
			// If startIndex is within bounds, return startIndex + 1, ensuring it does not exceed the length of 'this'.
			// Otherwise, return the length of 'this' as the next plausible index.
			return Math.Min(startIndex + 1, this.Length);
		}

		if (this.IsNode || find.IsNode)
		{
			var comparer = EqualityComparer<T>.Default;
			for (var i = startIndex; i >= 0; i--)
			{
				if (i + find.Length > this.Length) continue; // Skip if find exceeds bounds from i

				var match = true;
				for (var j = find.Length - 1; j >= 0 && match; j--)
				{
					// This check ensures we don't overshoot the bounds of 'this'
					if (i + j < this.Length)
					{
						match = comparer.Equals(this[i + j], find[j]);
					}
					else
					{
						match = false;
						break; // No need to continue if we're out of bounds
					}
				}

				if (match)
				{
					return i; // Found the last occurrence
				}
			}
			
			return -1;
		}
		else
		{
			// Finding a Leaf within another leaf.
			var i = this.data.Span[..(int)Math.Min(startIndex + 1, this.Length)].LastIndexOf(find.data.Span);
			if (i != -1)
			{
				return i;
			}

			return -1;
		}
	}

	[Pure]
	public long LastIndexOf(T find, int startIndex) => this.LastIndexOf(new Rope<T>(new[] { find }), startIndex);

	[Pure]
	public long LastIndexOf(T find) => this.LastIndexOf(new Rope<T>(new[] { find }));

	[Pure]
	public bool EndsWith(Rope<T> find)
	{
		var i = this.LastIndexOf(find); 
		return i != -1 && i == this.Length - find.Length;
	}

	[Pure]
	public bool EndsWith(ReadOnlyMemory<T> find) => this.EndsWith(new Rope<T>(find));

	/// <summary>
	/// Replaces all occurrences of the specified element with it's specified replacement.
	/// </summary>
	/// <param name="replace">The element to find.</param>
	/// <param name="with">The element to replace with.</param>
	/// <returns>A new rope with the replacements made.</returns>
	[Pure]
	public Rope<T> Replace(T replace, T with)
	{
		return this.Replace(new[] { replace }, new[] { with });
	}

	[Pure]
	public Rope<T> Replace(Rope<T> replace, Rope<T> with)
	{
		var accum = Empty;
		var remainder = this;
		var i = -1L;
		do
		{
			i = remainder.IndexOf(replace);
			if (i != -1)
			{
				accum += remainder.Slice(0, i);
				accum += with;
				remainder = remainder.Slice(i + replace.Length);
			}
		}
		while (i != -1);
		accum += remainder;
		return accum;
	}

	[Pure]
	public Rope<T> Replace<TEqualityComparer>(Rope<T> replace, Rope<T> with, TEqualityComparer comparer) where TEqualityComparer : IEqualityComparer<T>
	{
		var accum = Empty;
		var remainder = this;
		var i = -1L;
		do
		{
			i = remainder.IndexOf(replace, comparer);
			if (i != -1)
			{
				accum += remainder.Slice(0, i);
				accum += with;
				remainder = remainder.Slice(i + replace.Length);
			}
		}
		while (i != -1);
		accum += remainder;
		return accum;
	}

	/// <summary>
	/// Converts this to a string representation (if <typeparamref name="T"/> is a <see cref="char"/> a string of the contents is returned.)
	/// </summary>
	/// <returns>A string representation of the sequence.</returns>
	[Pure]
	public override string ToString()
	{
		if (this is Rope<char> rc)
		{
			return new string(rc.ToMemory().Span);
		}
		
		return typeof(T).Name + " x " + this.Depth;
	}

	/// <summary>
	/// Attempts to insert the given item in the correct sorted position, based on the comparer.
	/// NOTE: Due to potential fragmentation from InsertSorted, balancing is enforced.
	/// </summary>
	/// <typeparam name="TComparer">The comparer to use to sort with.</typeparam>
	/// <param name="item">The item to be inserted.</param>
	/// <param name="comparer">The comparer used to find the appropriate place to insert.</param>
	/// <returns>A new rope already balanced if necessary.</returns>
	[Pure]
	public Rope<T> InsertSorted<TComparer>(T item, TComparer comparer) where TComparer : IComparer<T>
	{
		var index = this.BinarySearch(item, comparer);
		if (index < 0)
		{ 
			index = ~index;
		}
		
		var (left, right) = this.SplitAt(index);
		// TODO: Common sorted prefix?
		var insert = new Rope<T>(new[] { item });
		return left + (insert + right);
		////return new Rope<T>(new Rope<T>(left, insert), right).Balanced();
	}

	/// <summary>
    /// Searches a slice of the sorted <see cref="Rope{T}"/> for a specified value using the specified <typeparamref name="TComparer"/> generic type.
	/// </summary>
	/// <typeparam name="TComparer">A type that implements <see cref="IComparer{T}"/>.</typeparam>
	/// <param name="item">The item to search for or compare to the desired insert location.</param>
	/// <param name="comparer">The comparer to used to find the correct index.</param>
	/// <returns>
	/// The zero-based index of value in the sorted rope, if value is found; otherwise,
	///  a negative number that is the bitwise complement of the index of the next element
	///  that is larger than value or, if there is no larger element, the bitwise complement
	///  of the Rope.Length.
	/// </returns>
    [Pure]
	public long BinarySearch<TComparer>(long index, int count, T item, IComparer<T> comparer) where TComparer : IComparer<T>
    {
		var offset = this.Slice(index, count).BinarySearch(item, comparer);
        return index + offset;
    }

	/// <summary>
    /// Searches an entire sorted <see cref="Rope{T}"/> for a specified value using the specified TComparer generic type.
	/// </summary>
	/// <typeparam name="TComparer">A type that implements <see cref="IComparer{T}"/>.</typeparam>
	/// <param name="item">The item to search for or compare to the desired insert location.</param>
	/// <param name="comparer">The comparer to used to find the correct index.</param>
	/// <returns>
	/// The zero-based index of value in the sorted rope, if value is found; otherwise,
	///  a negative number that is the bitwise complement of the index of the next element
	///  that is larger than value or, if there is no larger element, the bitwise complement
	///  of the Rope.Length.
	/// </returns>
    [Pure]
	public long BinarySearch<TComparer>(T item, TComparer comparer) where TComparer : IComparer<T>
    {
		if (this.IsNode)
		{
			var r = this.right.BinarySearch(item, comparer);
			if (r != -1)
			{
				return this.left.Length + r;
			}

			var l = this.left.BinarySearch(item, comparer);
			return l;
		}
		else
		{
        	return MemoryExtensions.BinarySearch(this.data.Span, item, comparer);
		}
    }

	/// <summary>
    /// Searches an entire sorted <see cref="Rope{T}"/> for a specified value using the specified TComparer generic type.
	/// </summary>
	/// <typeparam name="TComparer">A type that implements <see cref="IComparer{T}"/>.</typeparam>
	/// <param name="item">The item to search for or compare to the desired insert location.</param>
	/// <param name="comparer">The comparer to used to find the correct index.</param>
	/// <returns>
	/// The zero-based index of value in the sorted rope, if value is found; otherwise,
	///  a negative number that is the bitwise complement of the index of the next element
	///  that is larger than value or, if there is no larger element, the bitwise complement
	///  of the Rope.Length.
	/// </returns>
    [Pure]
	public long BinarySearch(T item)
    {
		if (this.IsNode)
		{
			var r = this.right.BinarySearch(item, Comparer<T>.Default);
			if (r != -1)
			{
				return this.left.Length + r;
			}

			var l = this.left.BinarySearch(item, Comparer<T>.Default);
			return l;
		}
		else
		{
        	return MemoryExtensions.BinarySearch(this.data.Span, item, Comparer<T>.Default);
		}
    }

	/// <summary>
	/// Gets the memory representation of this sequence, may allocate a new array if this instance is a tree.
	/// </summary>
	/// <returns>Read only memory of the rope.</returns>
	[Pure]
	public ReadOnlyMemory<T> ToMemory()
	{
		if (this.IsNode)
		{
			return this.ToArray().AsMemory();
		}
		
		return this.data;
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(obj, null))
		{
			return false;
		}
		
		return obj is Rope<T> other && this.Equals(other);
	}
	
	public static bool Equals(Rope<T>? a, Rope<T>? b) 
	{
		if (ReferenceEquals(a, null))
		{
			return ReferenceEquals(b, null);
		}

		return a.Equals(b);
	}

	/// <summary>
	/// Gets a value indicating whether these two ropes are equivalent in terms of their content.
	/// </summary>
	/// <param name="other">The other sequence to compare to</param>
	/// <returns>true if both instances hold the same sequence, otherwise false.</returns>
	[Pure]
	public bool Equals(Rope<T>? other)
	{
		if (ReferenceEquals(other, null))
		{
			return false;
		}
		
		if (this.Length != other.Length) 
		{
			return false;
		}
		
		return this.StartsWith(other);
	}
	
	/// <summary>
	/// Gets the content representative hash code of this rope's first element and it's length.
	/// NOTE: Combines some of the elements in the hash along with the rope length; This allows two sequences that have 
	/// different rope structures but the same representative string to have the same hash code.
	/// Theory goes that as strings get longer their length is more likely to differ, 
	/// shorter strings are fast to compare anyway.
	/// </summary>
	/// <returns>A hash code that represents the contents of the sequence, not the instance.</returns>
	[Pure]
	public override int GetHashCode() => this.Length switch 
	{
		> 256 => HashCode.Combine(this[0], this[this.Length / 2], this[this.Length - 1], this.Length),
		> 0 => HashCode.Combine(this[0], this.Length),
		_ => 0
	};

	public IEnumerator<T> GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

    [Pure]
	private bool CalculateIsBalanced()
    {
        return this.IsNode ?
            this.Depth < DepthToFibonnaciPlusTwo.Length && this.Length >= DepthToFibonnaciPlusTwo[this.Depth] :
            true;
    }

	/// <summary>
	/// Constructs a new Rope from a series of leaves into a tree.
	/// </summary>
	/// <param name="leaves">The leaf nodes to construct into a tree.</param>
	/// <returns>A new rope with the leaves specified.</returns>
	[Pure]
	public static Rope<T> Combine(Rope<Rope<T>> leaves) 
	{
		// Iteratively combine leaf nodes into a balanced tree
		while (leaves.Length > 1)
		{
			Rope<Rope<T>> parents = Rope<Rope<T>>.Empty;
			for (int i = 0; i < leaves.Length; i += 2)
			{
				// Combine two adjacent nodes into a parent node
				if (i + 1 < leaves.Length)
				{
					parents += new Rope<T>(leaves[i], leaves[i + 1]);
				}
				else
				{
					// For an odd number of nodes, the last one is moved up without a pair
					parents += leaves[i];
				}
			}

			// Prepare for the next iteration
			leaves = parents;
		}

		// The last remaining node is the root of the balanced tree
		return leaves.Length > 0 ? leaves[0] : Rope<T>.Empty;
	}

	/// <summary>
	/// Copies an enumerable sequence into a perfectly balanced binary tree.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="span"></param>
	/// <returns>A new rope.</returns>
    [Pure]
	public static Rope<T> FromEnumerable(IEnumerable<T> items)
    {
		// Create leaf nodes by chunking the sequence.
		var leaves = new Rope<Rope<T>>(
			items.Chunk(MaxLeafLength)
			.Select(array => new Rope<T>(array))
			.ToArray());
		
		// Build a tree out of leaves.
		return Combine(leaves);
    }

	/// <summary>
	/// Copies a span into a perfectly balanced binary tree.
	/// </summary>
	/// <param name="items">Read only list of items to copy from</param>
	/// <returns>A new rope or <see cref="Empty"/> if items is empty.</returns>
    [Pure]
	public static Rope<T> FromReadOnlyList(IReadOnlyList<T> items)
    {
		if (items.Count == 0)
		{
			return Rope<T>.Empty;
		}

		// Rope to hold all the constructed leaf nodes consecutively.
    	var leaves = Rope<Rope<T>>.Empty;

		// Create leaf nodes
		for (int i = 0; i < items.Count; i += MaxLeafLength)
		{
			var length = Math.Min(MaxLeafLength, items.Count - i);
			var memory = new T[length];
			for (var offset = 0; offset < length; offset++)
			{
				memory[offset] = items[(int)(i + offset)];
			}
			
			leaves += new Rope<T>(memory);
		}

		// Build a tree out of leaves.
		return Combine(leaves);
    }

	/// <summary>
	/// Copies a span into a perfectly balanced binary tree.
	/// </summary>
	/// <param name="span">The source items to read</param>
	/// <returns>A new rope or <see cref="Empty"/> if the span is empty.</returns>
    [Pure]
	public static Rope<T> FromReadOnlySpan(ref ReadOnlySpan<T> span)
    {
		if (span.Length == 0)
		{
			return Rope<T>.Empty;
		}

		// Rope to hold all the constructed leaf nodes consecutively.
    	var leaves = Rope<Rope<T>>.Empty;

		// Create leaf nodes
		for (int i = 0; i < span.Length; i += MaxLeafLength)
		{
			var length = Math.Min(MaxLeafLength, span.Length - i);
			var array = span.Slice(i, length).ToArray();
			leaves += new Rope<T>(array);
		}

		// Build a tree out of leaves.
		return Combine(leaves);
    }

	/// <summary>
	/// Creates a new subdivided rope tree without copying the memory. 
	/// This is distinct from constructing a rope with new Rope{T}(memory) as this pre-subdivides the rope for editing.
	/// </summary>
	/// <param name="memory">The source memory to point to.</param>
	/// <returns>A new rope or <see cref="Empty"/> if the memory is empty.</returns>
    [Pure]
	public static Rope<T> FromMemory(ReadOnlyMemory<T> memory)
    {
		if (memory.Length == 0)
		{
			return Rope<T>.Empty;
		}

		// Rope to hold all the constructed leaf nodes consecutively.
    	var leaves = Rope<Rope<T>>.Empty;

		// Create leaf nodes
		for (int i = 0; i < memory.Length; i += MaxLeafLength)
		{
			var length = Math.Min(MaxLeafLength, memory.Length - i);
			leaves += new Rope<T>(memory.Slice(i, length));
		}

		return Combine(leaves);
    }

	[Pure]
    public Rope<T> Clear() => Rope<T>.Empty;

	[Pure]
    IImmutableList<T> IImmutableList<T>.Add(T value) => this.Add(value);

	[Pure]
    IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items) => this.AddRange(items.ToRope());

	[Pure]
	IImmutableList<T> IImmutableList<T>.Clear() => Rope<T>.Empty;

	[Pure]
    int IImmutableList<T>.IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
	{
		throw new NotImplementedException();
	}

	[Pure]
    IImmutableList<T> IImmutableList<T>.Insert(int index, T element) => this.Insert(index, element);

	[Pure]
    IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items) => this.InsertRange(index, items.ToArray());

	[Pure]
    int IImmutableList<T>.LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
    {
        throw new NotImplementedException();
    }

	[Pure]
    IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T>? equalityComparer)
    {
        throw new NotImplementedException();
    }

	[Pure]
    IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
    {
        throw new NotImplementedException();
    }

	[Pure]
    IImmutableList<T> IImmutableList<T>.RemoveAt(int index) => this.RemoveAt((int)index);

	[Pure]
    IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
    {
        throw new NotImplementedException();
    }

	[Pure]
    IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count) => this.RemoveRange((int)index, (int)count);

	[Pure]
    IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer) => this.Replace(new[] { oldValue }, new[] { newValue }, equalityComparer ?? EqualityComparer<T>.Default);

	[Pure]
    IImmutableList<T> IImmutableList<T>.SetItem(int index, T value) => this.SetItem((int)index, value);

    private struct Enumerator : IEnumerator<T>
	{
		private readonly Rope<T> rope;
		private int index;
		public Enumerator(Rope<T> rope)
		{
			this.rope = rope;
			this.index = -1;
		}
		
		public T Current => this.rope.ElementAt(this.index);

		object IEnumerator.Current => this.rope.ElementAt(this.index);

		public void Dispose()
		{
			this.index = -2;
		}

		public bool MoveNext()
		{
			if (this.index == -2)
			{
				throw new ObjectDisposedException(nameof(Enumerator));
			}

			this.index++;
			return this.index < this.rope.Length;
		}

		public void Reset()
		{
			this.index = -1;
		}
    }
}
