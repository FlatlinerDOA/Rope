
namespace Rope;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Diagnostics.Metrics;

/// <summary>
/// A rope is an immutable sequence built using a b-tree style data structure that is useful for efficiently applying and storing edits, most commonly to text, but any list or sequence can be edited.
/// </summary>
public sealed record Rope<T> : IEnumerable<T> where T : IEquatable<T>
{
	public const int MaxTreeDepth = 46;

	public const int LargeObjectHeapBytes = 85_000 - 24;

  	private static readonly Meter meter = new Meter("rope");

    private static readonly Counter<int> rebalances = meter.CreateCounter<int>($"rope.{typeof(T).Name}.rebalances");

	/// <summary>
	/// Static size of the maximum length of a leaf node in the rope's binary tree. This is used for balancing the tree.
	/// </summary>
	public static readonly int MaxLeafLength = LargeObjectHeapBytes / Unsafe.SizeOf<T>();

	// Defines the empty leaf.
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
	/// Creates a new instance of Rope{T}.
	/// </summary>
	public Rope()
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

				this.IsBalanced = this.IsNode ?
					this.Depth < DepthToFibonnaciPlusTwo.Length ?
						this.Length >= DepthToFibonnaciPlusTwo[this.Depth] : 
						this.Left.IsBalanced && this.Right.IsBalanced :
					true;

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

				this.IsBalanced = this.IsNode ?
					this.Depth < DepthToFibonnaciPlusTwo.Length ?
						this.Length >= DepthToFibonnaciPlusTwo[this.Depth] : 
						this.Left.IsBalanced && this.Right.IsBalanced :
					true;
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
			this.IsBalanced = this.IsNode ?
				this.Depth < DepthToFibonnaciPlusTwo.Length ?
					this.Length >= DepthToFibonnaciPlusTwo[this.Depth] : 
					this.Left.IsBalanced && this.Right.IsBalanced :
				true;

			// if (newDepth > MaxTreeDepth)
			// {
			// 	// This is the only point at which we are actually increasing the depth beyond the limit, this is where a re-balance may be necessary.
			// 	this.left = left.Balanced();
			// 	this.right = right.Balanced();
			// 	this.Depth =  Math.Max(this.left.Depth, this.right.Depth) + 1;
			// }
		}
	}
	
	public T this[int index] => this.ElementAt(index);

	/// <summary>
	/// Gets a range of elements in the form of a new instance of <see cref="Rope{T}"/>.
	/// </summary>
	/// <param name="range">The range to select.</param>
	/// <returns>A new rope instance if the slice does not cover the entire sequence, otherwise returns the original instance.</returns>
	public Rope<T> this[Range range]
	{
		get
		{
			var (offset, length) = range.GetOffsetAndLength(this.Length);
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
	public int Weight => this.left?.Length ?? this.data.Length;

	/// <summary>
	/// Gets the length of the rope in terms of the number of elements it contains.
	/// </summary>
	public int Length => this.IsNode ? this.left.Length + this.right.Length : this.data.Length;

	/// <summary>
	/// Gets the maximum depth of the tree, returns 0 if this is a leaf.
	/// </summary>
	public int Depth { get; }

	/// <summary>
	/// Gets the element at the specified index.
	/// </summary>
	/// <param name="index"></param>
	/// <returns>The element at the specified index.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if index is larger than or equal to the length or less than 0.</exception>
	public T ElementAt(int index)
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

			throw new ArgumentOutOfRangeException(nameof(index));
		}

		return this.data.Span[index];
	}

	/// <summary>
	/// Gets an enumerable of slices of this rope, splitting by the given separator element.
	/// </summary>
	/// <param name="separator">The element to separate by, this element will never be included in the returned sequence.</param>
	/// <returns>Zero or more ropes splitting the rope by it's separator.</returns>
	public IEnumerable<Rope<T>> Split(T separator)
	{
		Rope<T> remainder = this;
		do
		{
			int i = remainder.IndexOf(separator);
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
	public IEnumerable<Rope<T>> Split(ReadOnlyMemory<T> separator)
	{
		Rope<T> remainder = this;
		do
		{
			int i = remainder.IndexOf(separator);

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
	/// <param name="i"></param>
	/// <returns></returns>
	public (Rope<T> Left, Rope<T> Right) SplitAt(int i)
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
				var (a, b) = (this.left ?? Rope<T>.Empty).SplitAt(i);
				return (a, new Rope<T>(b, this.right ?? Rope<T>.Empty));
			}
			else
			{
				var (a, b) = (this.right ?? Rope<T>.Empty).SplitAt(i - this.Weight);
				return (new Rope<T>(this.left ?? Rope<T>.Empty, a), b);
			}
		}

		return (new Rope<T>(this.data.Slice(0, i)), new Rope<T>(this.data.Slice(i)));
	}

	/// <summary>
	/// Inserts a sequence of elements at the given index.
	/// </summary>
	/// <param name="index">The index to insert at.</param>
	/// <param name="items">The items to insert at the given index.</param>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the items added.</returns>
	public Rope<T> Insert(int index, ReadOnlyMemory<T> items)
	{
		return this.Insert(index, new Rope<T>(items));
	}

	/// <summary>
	/// Inserts a sequence of elements at the given index.
	/// </summary>
	/// <param name="index">The index to insert at.</param>
	/// <param name="items">The items to insert at the given index.</param>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the items added.</returns>
	public Rope<T> Insert(int index, Rope<T> items)
	{
		if (index > this.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		var (left, right) = this.SplitAt(index);
		return new Rope<T>(left, new Rope<T>(items, right));
	}

	/// <summary>
	/// Removes a subset of elements for a given range.
	/// </summary>
	/// <param name="start">The start index to remove from.</param>
	/// <param name="length">The number of items to be removed.</param>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the items removed if length is non-zero. Otherwise returns the original instance.</returns>
	public Rope<T> Remove(int start, int length)
	{
		if (length == 0)
		{
			return this;
		}

		return new Rope<T>(this.Slice(0, start), this.Slice(start + length));
	}

	/// <summary>
	/// Removes the tail range of elements from a given starting index (Alias for Slice).
	/// </summary>
	/// <param name="start">The start index to remove from.</param>
	/// <returns>A new instance of <see	cref="Rope{T}"/> with the the items removed if start is non-zero. Otherwise returns the original instance.</returns>
	public Rope<T> Remove(int start) => this.Slice(start, this.Length - start);

	/// <summary>
	/// Concatenates two sequences together into a single sequence.
	/// Performance note: This may attempt to perform a balancing if tree depth exceeds maximum tree depth.
	/// </summary>
	/// <param name="source">The source rope to append to this rope.</param>
	/// <returns></returns>
	public Rope<T> Concat(Rope<T> source)
	{
		if (source.Length == 0)
		{
			return this;
		}

		if (this.Length == 0)
		{
			return source;
		}

		if (this.Depth >= MaxTreeDepth)
		{
			return new Rope<T>(this.Balanced(), source);
		}
		
		return new Rope<T>(this, source);
	}

	/// <summary>
	/// Gets a range of elements in the form of a new instance of <see cref="Rope{T}"/>.
	/// </summary>
	/// <param name="start">The start to select from.</param>
	/// <returns>A new rope instance if start is non-zero, otherwise returns the original instance.</returns>
	public Rope<T> Slice(int start)
	{
		return this.Slice(start, this.Length - start);
	}

	/// <summary>
	/// Gets a range of elements in the form of a new instance of <see cref="Rope{T}"/>.
	/// </summary>
	/// <param name="start">The start to select from.</param>
	/// <param name="length">The number of elements to return.</param>
	/// <returns>A new rope instance if the slice does not cover the entire sequence, otherwise returns the original instance.</returns>
	public Rope<T> Slice(int start, int length)
	{
		if (start == 0 && length == this.Length)
		{
			return this;
		}

		var (head, _) = this.SplitAt(start + length);
		return head.SplitAt(start).Right;
	}

	/// <summary>
	/// Concatenates two rope instances together into a single sequence.
	/// </summary>
	/// <param name="a">The first sequence.</param>
	/// <param name="b">The second sequence.</param>
	/// <returns>A new rope instance concatenating the two sequences.</returns>
	public static Rope<T> operator +(Rope<T> a, Rope<T> b) => a.Concat(b);

	/// <summary>
	/// Appends an element to the existing instance.
	/// </summary>
	/// <param name="a">The first sequence.</param>
	/// <param name="b">The second element to append to the sequence.</param>
	/// <returns>A new rope instance concatenating the two sequences.</returns>
	public static Rope<T> operator +(Rope<T> a, T b) => a.Concat(new Rope<T>(new T[] { b }.AsMemory()));

	/// <summary>
	/// Implicitly converts a read only memory sequence into a rope.
	/// </summary>
	/// <param name="a">A section of memory to wrap in a rope instance.</param>
	public static implicit operator Rope<T> (ReadOnlyMemory<T> a) => new Rope<T>(a);
	public static implicit operator Rope<T> (T[] a) => new Rope<T>(a);

	public static explicit operator Rope<T> (ReadOnlySpan<T> a) => new Rope<T>(a.ToArray());

	/// <summary>
	/// Determines if the rope's binary tree is unbalanced and then recursively rebalances if necessary.
	/// </summary>
	/// <returns>A balanced tree or the original rope if not out of range.</returns>
	public Rope<T> Balanced()
	{
		if (!this.IsBalanced)
		{
			rebalances.Add(1);
			if (this.IsNode && this.Length > MaxLeafLength)
			{
				// Recursively balance if we are already very long.
				return new Rope<T>(this.Left.Balanced(), this.Right.Balanced());
			}
			
			// If short enough brute force rebalance into a leaf.
			return new Rope<T>(this.ToMemory());
		}

		return this;
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
	public int CommonPrefixLength(Rope<T> other)
	{
		if (!this.IsNode && !other.IsNode)
		{
			return MemoryExtensions.CommonPrefixLength(this.data.Span, other.data.Span);
		}

		// TODO: Compare Performance
		////return MemoryExtensions.CommonPrefixLength(this.ToMemory().Span, other.ToMemory().Span);

		// Performance analysis: https://neil.fraser.name/news/2007/10/09/
		var n = Math.Min(this.Length, other.Length);
		for (int i = 0; i < n; i++)
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
	public int CommonSuffixLength(Rope<char> other)
	{
		// Performance analysis: https://neil.fraser.name/news/2007/10/09/
		var n = Math.Min(this.Length, other.Length);
		for (int i = 1; i <= n; i++)
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
	public T[] ToArray()
	{
		if (this.IsNode)
		{
			// Instead of: new T[this.left.Length + this.right.Length]; we use an uninitalized array as we are copying over the entire contents.
			var result = GC.AllocateUninitializedArray<T>(this.left.Length + this.right.Length);
			var mem = result.AsMemory();
			this.left.CopyTo(mem);
			this.right.CopyTo(mem.Slice(this.left.Length));
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
			this.right.CopyTo(other.Slice(this.left.Length));
		}
		else
		{
			// Leaf node so copy memory.
			this.data.CopyTo(other);
		}
	}
	
	/// <summary>
	/// Finds the index of a subset of the rope
	/// </summary>
	/// <param name="find"></param>
	/// <returns>A number greater than or equal to zero if the sub-sequence is found, otherwise returns -1.</returns>
	public int IndexOf(Rope<T> find)
	{
		if (this.IsNode)
		{
			// We may have a fun boundary condition here. 
			var comparer = EqualityComparer<T>.Default;
			if (find.IsNode)
			{
				// Try and find in left half
				var index = this.left.IndexOf(find);
				if (index != -1)
				{
					return index;
				}

				var startIndex = Math.Max(0, this.left.Length + 1 - find.Length);

				// Check in the 'left' array for a starting match that could spill over to 'right'
				for (int i = startIndex; i < left.Length; i++)
				{
					bool match = true;
					for (int j = 0; j < find.Length && match; j++)
					{
						if (i + j < left.Length)
						{
							match = comparer.Equals(this.left[i + j], find[j]);
						}
						else
						{
							match = comparer.Equals(this.right[i + j - left.Length], find[j]);
						}
					}

					if (match)
					{
						return i;
					}
				}


				index = this.right.IndexOf(find);
				if (index != -1)
				{
					return this.left.Length + index;
				}
				
				return -1;
			}

			// This is a node, but leaf is memory, we have to go element by element.
			ReadOnlySpan<T> findSpan = find.data.Span;
			
			// Check in the 'left' array for a starting match that could spill over to 'right'
			for (int i = 0; i < this.left.Length; i++)
			{
				bool match = true;
				for (int j = 0; j < findSpan.Length && match; j++)
				{
					if (i + j < left.Length)
					{
						match = comparer.Equals(this.left[i + j], findSpan[j]);
					}
					else
					{
						match = comparer.Equals(this.right[i + j - left.Length], findSpan[j]);
					}
				}

				if (match)
				{
					return i;
				}
			}

			// Check in the 'right' array, but only if the 'find' can be fully contained within 'right'
			for (int i = 0; i <= right.Length - findSpan.Length; i++)
			{
				bool match = true;
				for (int j = 0; j < find.Length; j++)
				{
					match = match && comparer.Equals(right[i + j], find[j]);
				}

				if (match)
				{
					return this.left.Length + i;
				}
			}

			// Indicate that no match was found
			return -1;
		}
		else 
		{
			return this.data.Span.IndexOf(find.data.Span);
		}
	}


	public int IndexOf(Rope<T> find, int offset)
	{
		var i = this.Slice(offset).IndexOf(find);
		if (i != -1)
		{
			return i + offset;			
		}

		return -1;
	}

	public int IndexOf(ReadOnlyMemory<T> find)
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
			return this.data.Span.IndexOf(find.Span);
		}
		
		return -1;
	}

	public int IndexOf(T find)
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

	public int IndexOf(T find, int offset)
	{
		var i = this.Slice(offset).IndexOf(find);
		if (i != -1)
		{
			return i + offset;
		}
		
		return -1;
	}

	public int IndexOf(ReadOnlyMemory<T> find, int offset)
	{
		var i = this.Slice(offset).IndexOf(find);
		if (i != -1)
		{
			return i + offset;
		}

		return -1;
	}

	public bool StartsWith(Rope<T> find) => this.IndexOf(find) == 0;

	public bool StartsWith(ReadOnlyMemory<T> find) => this.StartsWith(new Rope<T>(find));

	public int LastIndexOf(Rope<T> find, int offset = 0)
	{
		if (find.Length > this.Length - offset)
		{
			return -1;
		}

		if (this.IsNode || find.IsNode)
		{
			var comparer = EqualityComparer<T>.Default;
			var fend = find.Length - 1; // End position of find.
			var matched = false;
			var start = this.Length - 1 - offset;
			for (int i = start; i >= 0; i--)
			{
				if (comparer.Equals(this[i], find[fend]))
				{
					matched = true;
					fend--;
					if (fend < 0)
					{
						return i;
					}
				}
				else if (matched)
				{
					matched = false;
					fend = find.Length - 1; // Start again
				}
			}
			
			return -1;
		}
		else
		{
			// Finding a Leaf within another leaf.
			return this.data.Span.Slice(0, this.data.Span.Length - offset).LastIndexOf(find.data.Span);
		}
	}

	public int LastIndexOf(ReadOnlyMemory<T> find, int offset = 0) => this.LastIndexOf(new Rope<T>(find), offset);

	public int LastIndexOf(T find, int offset = 0) => this.LastIndexOf(new Rope<T>(new[] { find }), offset);

	public bool EndsWith(Rope<T> find)
	{
		var i = this.LastIndexOf(find); 
		return i != -1 && i == this.Length - find.Length;
	}

	public bool EndsWith(ReadOnlyMemory<T> find) => this.EndsWith(new Rope<T>(find));

	/// <summary>
	/// Replaces all occurrences of the specified element with it's specified replacement.
	/// </summary>
	/// <param name="replace">The element to find.</param>
	/// <param name="with">The element to replace with.</param>
	/// <returns>A new rope with the replacements made.</returns>
	public Rope<T> Replace(T replace, T with)
	{
		return this.Replace(new[] { replace }.AsMemory(), new[] { with });
	}

	public Rope<T> Replace(ReadOnlyMemory<T> replace, ReadOnlyMemory<T> with)
	{
		var accum = Empty;
		var remainder = this;
		var i = -1;
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

	/// <summary>
	/// Converts this to a string representation (if <typeparamref name="T"/> is a <see cref="char"/> a string of the contents is returned.)
	/// </summary>
	/// <returns>A string representation of the sequence.</returns>
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
	public Rope<T> InsertSorted<TComparer>(T item, TComparer comparer) where TComparer : IComparer<T>
	{
		var index = ~this.BinarySearch(item, comparer);
		var left = this.Slice(0, index);
		var right = this.Slice(index);
		var insert = new Rope<T>(new[] { item });
		return new Rope<T>(new Rope<T>(left, insert), right).Balanced();
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
    public int BinarySearch<TComparer>(int index, int count, T item, IComparer<T> comparer) where TComparer : IComparer<T>
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
    public int BinarySearch<TComparer>(T item, TComparer comparer) where TComparer : IComparer<T>
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
    public int BinarySearch(T item)
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
	public ReadOnlyMemory<T> ToMemory()
	{
		if (this.IsNode)
		{
			return this.ToArray().AsMemory();
		}
		
		return this.data;
	}

	/// <summary>
	/// Gets a value indicating whether these two ropes are equivalent in terms of their content.
	/// </summary>
	/// <param name="other">The other sequence to compare to</param>
	/// <returns>true if both instances hold the same sequence, otherwise false.</returns>
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
	/// </summary>
	/// <returns>A hash code that represents the contents of the sequence, not the instance.</returns>
	public override int GetHashCode()
	{
		// AC: Combines first element hash with rope length; This allows two sequences that have 
		// different rope structures but the same representative string to have the same hash code.
		// Theory goes that as strings get longer their length is more likely to differ, 
		// shorter strings are fast to compare anyway.
		var stringHash = 0;
		if (this.Length > 0)
		{
			stringHash = this.ElementAt(0).GetHashCode();
		}
		
		return HashCode.Combine(stringHash, this.Length.GetHashCode());
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

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
