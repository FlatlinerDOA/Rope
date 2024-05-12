// Copyright 2024 Andrew Chisholm (https://github.com/FlatlinerDOA)

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
using System.Diagnostics;
using System.Text;
using System.Buffers;
using Rope.Compare;
using System.Runtime.InteropServices;

public enum RopeSplitOptions
{
    /// <summary>
    /// Excludes the separator from the results, includes empty results.
    /// </summary>
    None = 0,

    /// <summary>
    /// Excludes the separator and excludes empty results.
    /// </summary>
    RemoveEmpty = 1,

    /// <summary>
    /// Includes the separator at the end of each result (except the last), empty results are not possible.
    /// </summary>
    SplitAfterSeparator = 2,

    /// <summary>
    /// Includes the separator at the start of each result (except the first), empty results are not possible.
    /// </summary>
    SplitBeforeSeparator = 3,
}

/// <summary>
/// A rope is an immutable sequence built using a b-tree style data structure that is useful for efficiently applying and storing edits, most commonly to text, but any list or sequence can be edited.
/// </summary>
public readonly record struct Rope<T> : IEnumerable<T>, IReadOnlyList<T>, IImmutableList<T>, IEquatable<Rope<T>> where T : IEquatable<T>
{
    private static readonly ArrayPool<ReadOnlyMemory<T>> BufferPool = ArrayPool<ReadOnlyMemory<T>>.Create(128, 16);

    private readonly record struct RopeNode(Rope<T> left, Rope<T> right);

    /// <summary>
    /// Maximum tree depth allowed.
    /// </summary>
    public const int MaxTreeDepth = 46;

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
    public static readonly int MaxLeafLength = RopeExtensions.CalculateAlignedBufferLength<T>();

    /// <summary>
    /// Gets the maximum number of elements any rope can have. 
    /// For <see cref="byte"/> this is 5,979,654,405,241,176,064 bytes (5,311 petabytes).
    /// For <see cref="char"/> this is 2,989,827,202,620,588,032 characters.
    /// For <see cref="int"/> this is 1,494,913,601,310,294,016 ints.
    /// </summary>
    public static readonly long MaxLength = 2.IntPow(MaxTreeDepth) * MaxLeafLength;

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

    private readonly object data;

    /// <summary>
    /// Creates a new instance of Rope{T}. Use Empty instead.
    /// </summary>
    public Rope()
    {
        // Empty rope is just a leaf node.
        this.data = ReadOnlyMemory<T>.Empty;
        this.IsBalanced = true;
        this.BufferCount = 1;
    }

    public Rope(T value)
    {
        // Single value needs to be ValueTuple to be able to hold nullable references.
        this.data = ValueTuple.Create(value);
        this.IsBalanced = true;
        this.Length = 1;
        this.BufferCount = 1;
    }

    // TODO: Try two element, and maybe three element tuples.
    //public Rope(T a, T b)
    //{
    //    // Two values is just a leaf node.
    //    this.data = ValueTuple.Create(a, b);
    //    this.IsBalanced = true;
    //    this.Length = 2;
    //    this.BufferCount = 1;
    //}

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
        this.data = data.Length == 1 ? ValueTuple.Create(data.Span[0]) : data;
        this.IsBalanced = true;
        this.Length = data.Length;
        this.BufferCount = 1;
    }

    /// <summary>
    /// Creates a new instance of Rope{T}.
    /// </summary>
    /// <param name="left">The left child node.</param>
    /// <param name="right">The right child node.</param>
    /// <exception cref="ArgumentNullException">Thrown if either the left or right node is null.</exception>
    internal Rope(Rope<T> left, Rope<T> right)
    {
        if (right.Length == 0)
        {
            if (left.data is RopeNode leftNode)
            {
                var node = leftNode;
                this.data = node;
                this.Depth = Math.Max(node.left.Depth, node.right.Depth) + 1;
                this.Length = left.Length;
                this.BufferCount = node.left.BufferCount + node.right.BufferCount;
                this.IsBalanced = CalculateIsBalanced(this.Length, this.Depth);
            }
            else if (left.data is ReadOnlyMemory<T> leftData)
            {
                if (leftData.Length == 1)
                {
                    this.data = ValueTuple.Create(leftData.Span[0]);
                }
                else
                {
                    this.data = leftData;
                }

                this.BufferCount = 1;
                this.Length = leftData.Length;
                this.IsBalanced = true;
            }
            else if (left.data is ValueTuple<T> value)
            {
                this.data = value;
                this.BufferCount = 1;
                this.Length = 1;
                this.IsBalanced = true;
            }
            else
            {
                this.data = ReadOnlyMemory<T>.Empty;
                this.IsBalanced = true;
            }
        }
        else if (left.Length == 0)
        {
            if (right.data is RopeNode rightNode)
            {
                this.data = new RopeNode(rightNode.left, rightNode.right);
                this.Depth = Math.Max(rightNode.left.Depth, rightNode.right.Depth) + 1;
                this.Length = right.Length;
                this.BufferCount = rightNode.left.BufferCount + rightNode.right.BufferCount;
                this.IsBalanced = CalculateIsBalanced(this.Length, this.Depth);
            }
            else if (right.data is ValueTuple<T> value)
            {
                this.data = value;
                this.BufferCount = 1;
                this.Length = 1;
                this.IsBalanced = true;
            }
            else if (right.data is ReadOnlyMemory<T> rightData)
            {
                if (rightData.Length == 1)
                {
                    this.data = ValueTuple.Create(rightData.Span[0]);
                }
                else
                {
                    this.data = rightData;
                }

                this.BufferCount = 1;
                this.Length = rightData.Length;
                this.IsBalanced = true;
            }
            else
            {
                this.data = ReadOnlyMemory<T>.Empty;
                this.IsBalanced = true;
            }
        }
        else
        {
            this.data = new RopeNode(left, right);
            this.Depth = Math.Max(left.Depth, right.Depth) + 1;
            Debug.Assert(this.Depth < MaxTreeDepth, "Too deep!");
            this.Length = left.Length + right.Length;
            this.BufferCount = left.BufferCount + right.BufferCount;
            this.IsBalanced = CalculateIsBalanced(this.Length, this.Depth);

            // if (newDepth > MaxTreeDepth)
            // {
            // 	// This is the only point at which we are actually increasing the depth beyond the limit, this is where a re-balance may be necessary.
            // 	this.left = left.Balanced();
            // 	this.right = right.Balanced();
            // 	this.Depth =  Math.Max(this.left.Depth, this.right.Depth) + 1;
            // }
        }

        Debug.Assert(this.data is ReadOnlyMemory<T> or RopeNode or ValueTuple<T>, "Bad data type");
    }

    public readonly T this[long index] => this.ElementAt(index);

    /// <summary>
    /// Defines how many leaf node buffers this rope contains.
    /// </summary>
    public int BufferCount { get; }

    /// <summary>
    /// Gets a range of elements in the form of a new instance of <see cref="Rope{T}"/>.
    /// </summary>
    /// <param name="range">The range to select.</param>
    /// <returns>A new rope instance if the slice does not cover the entire sequence, otherwise returns the original instance.</returns>
    public readonly Rope<T> this[Range range]
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
    public Rope<T>? Left => this.data is RopeNode n ? n.left : default;

    /// <summary>
    /// Gets the right or suffix branch of the rope. May be null if this is a leaf node.
    /// </summary>
    public Rope<T>? Right => this.data is RopeNode n ? n.right : default;

    /// <summary>
    /// Gets a value indicating whether this is a Node and Left and Right will be non-null, 
    /// otherwise it is a leaf and just wraps a slice of read only memory.
    /// </summary>
    [MemberNotNullWhen(true, nameof(this.Left))]
    [MemberNotNullWhen(true, nameof(this.Right))]
    public bool IsNode => this.Depth != 0;

    /// <summary>
    /// Gets the length of the left Node if this is a node (the split-point essentially), otherwise the length of the data. 
    /// </summary>
    public long Weight => this.data switch
    {
        ValueTuple<T> => 1,
        RopeNode n => n.left.Length,
        ReadOnlyMemory<T> m => m.Length,
        _ => 0
    };

    /// <summary>
    /// Gets the length of the rope in terms of the number of elements it contains.
    /// </summary>
    public long Length { get; }

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T ElementAt(long index) =>
        this.data switch
        {
            ValueTuple<T> value => index == 0 ? value.Item1 : throw new IndexOutOfRangeException(nameof(index)),
            ReadOnlyMemory<T> memory => memory.Span[(int)index],
            RopeNode node => node.left.Length <= index ? node.right.ElementAt(index - node.left.Length) : node.left.ElementAt(index),
            _ => throw new IndexOutOfRangeException(nameof(index)),
        };

    /// <summary>
    /// Gets an enumerable of slices of this rope, splitting by the given separator element.
    /// </summary>
    /// <param name="separator">The element to separate by, this element will never be included in the returned sequence.</param>
    /// <returns>Zero or more ropes splitting the rope by it's separator.</returns>
    [Pure]
    public readonly IEnumerable<Rope<T>> Split(T separator)
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
    /// <param name="options">Optional settings for how to deal with the separator itself.</param>
    /// <returns>Zero or more ropes splitting the rope by it's separator.</returns>
    [Pure]
    public readonly IEnumerable<Rope<T>> Split(ReadOnlyMemory<T> separator, RopeSplitOptions options = RopeSplitOptions.None)
    {
        Rope<T> remainder = this;
        do
        {
            var i = remainder.IndexOf(separator);
            if (i != -1)
            {
                var chunk = remainder.Slice(
                    0,
                    options switch
                    {
                        RopeSplitOptions.None => i,
                        RopeSplitOptions.SplitBeforeSeparator => i,
                        RopeSplitOptions.SplitAfterSeparator => i + separator.Length,
                        _ => throw new NotImplementedException(),
                    });
                if (!chunk.IsEmpty || options == RopeSplitOptions.None)
                {
                    yield return chunk;
                }

                remainder = remainder.Slice(
                    options switch
                    {
                        RopeSplitOptions.None => i + separator.Length,
                        RopeSplitOptions.SplitBeforeSeparator => i,
                        RopeSplitOptions.SplitAfterSeparator => i + separator.Length,
                        _ => throw new NotImplementedException(),
                    });
            }
            else
            {
                if (!remainder.IsEmpty || options == RopeSplitOptions.None)
                {
                    yield return remainder;
                }

                yield break;
            }
        }
        while (true);
    }

    public readonly (Rope<T> Left, Rope<T> Right) SplitAt(long i) => this.SplitAt(i, 100);

    /// <summary>
    /// Splits a rope in two to pieces a left and a right half at the specified index.
    /// </summary>
    /// <param name="i">The index to split the two halves at.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if index to split at is out of range.</exception>
    /// <returns></returns>
    [Pure]
    private readonly (Rope<T> Left, Rope<T> Right) SplitAt(long i, int recursionRemaining)
    {
        Debug.Assert(recursionRemaining > 0, $"Infinite loop? {this.Depth} {this.Length} {i}");
        switch (this.data)
        {
            case ValueTuple<T>:
                return i == 0 ? (Empty, this) :
                    i == 1 ? (this, Empty) :
                    throw new ArgumentOutOfRangeException(nameof(i));
            case ReadOnlyMemory<T> m:
                return (new Rope<T>(m[..(int)i]), new Rope<T>(m[(int)i..]));
            case RopeNode node:
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
                    var (newLeft, newRight) = node.left.SplitAt(i, --recursionRemaining);
                    return (newLeft, new Rope<T>(newRight, node.right));
                }
                else
                {
                    var (a, b) = node.right.SplitAt(i - this.Weight, --recursionRemaining);
                    return (new Rope<T>(node.left, a), b);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary>
    /// Replaces a single element at the given index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="item">The item to replace with at the given index.</param>
    /// <returns>A new instance of <see	cref="Rope{T}"/> with the the item included as a replacement.</returns>
    [Pure]
    public readonly Rope<T> SetItem(long index, T item)
    {
        var (left, right) = this.SplitAt(index);
        return new Rope<T>(left, new Rope<T>(new Rope<T>(item), right.Slice(1))).Balanced();
    }

    /// <summary>
    /// Inserts a single element at the given index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="items">The items to insert at the given index.</param>
    /// <returns>A new instance of <see	cref="Rope{T}"/> with the the items added.</returns>
    [Pure]
    public readonly Rope<T> Insert(long index, T item)
    {
        return this.InsertRange(index, new Rope<T>(item));
    }

    /// <summary>
    /// Inserts a sequence of elements at the given index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="items">The items to insert at the given index.</param>
    /// <returns>A new instance of the full <see cref="Rope{T}"/> with the the items added at the specified index.</returns>
    [Pure]
    public readonly Rope<T> InsertRange(long index, ReadOnlyMemory<T> items)
    {
        return this.InsertRange(index, new Rope<T>(items));
    }

    /// <summary>
    /// Inserts a sequence of elements at the given index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="items">The items to insert at the given index.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if start is greater than Length, or start + length exceeds the Length.</exception>
    /// <returns>A new instance of <see	cref="Rope{T}"/> with the the items added.</returns>
    [Pure]
    public readonly Rope<T> InsertRange(long index, Rope<T> items)
    {
        if (index > this.Length)
        {
            throw new IndexOutOfRangeException(nameof(index));
        }

        var (left, right) = this.SplitAt(index);
        return new Rope<T>(left, new Rope<T>(items, right)).Balanced();
    }

    /// <summary>
    /// Removes a subset of elements for a given range.
    /// </summary>
    /// <param name="start">The start index to remove from.</param>
    /// <param name="length">The number of items to be removed.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if start is greater than Length, or start + length exceeds the Length.</exception>
    /// <returns>A new instance of <see	cref="Rope{T}"/> with the the items removed if length is non-zero. Otherwise returns the original instance.</returns>
    [Pure]
    public readonly Rope<T> RemoveRange(long start, long length)
    {
        if (length == 0)
        {
            return this;
        }

        if (start == 0 && length == this.Length)
        {
            return Empty;
        }

        return new Rope<T>(this.RemoveRange(start), this.Slice(start + length));
    }

    /// <summary>
    /// Removes the tail range of elements from a given starting index.
    /// </summary>
    /// <param name="startIndex">The start index to remove from (inclusive).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if start is greater than Length.</exception>
    /// <returns>A new instance of <see	cref="Rope{T}"/> with the the items removed if start is non-zero. Otherwise returns the original instance.</returns>
    [Pure]
    public readonly Rope<T> RemoveRange(long startIndex)
    {
        if (startIndex == 0)
        {
            return Empty;
        }

        if (startIndex == this.Length)
        {
            return this;
        }

        switch (this.data)
        {
            // NOTE: ValueTuple<T> is an impossible case because startIndex would have to be either 0 or 1, if not it's out of range.
            case ReadOnlyMemory<T> m:
                return new Rope<T>(m[..(int)startIndex]);
            case RopeNode node:
                if (startIndex <= this.Weight)
                {
                    return node.left.RemoveRange(startIndex);
                }
                else
                {
                    return new Rope<T>(node.left, node.right.RemoveRange(startIndex - this.Weight));
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(startIndex));
        }
    }

    /// <summary>
    /// Removes a single element at the specified index.
    /// </summary>
    /// <param name="index">The index to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if index is greater than or equal to Length or less than zero.</exception>
    /// <returns>A new instance of <see	cref="Rope{T}"/> with the single item removed if index is valid.</returns>
    [Pure]
    public readonly Rope<T> RemoveAt(long index)
    {
        if (index < 0 || index >= this.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index == this.Length)
        {
            return this;
        }

        return new Rope<T>(this.RemoveRange(index), this.Slice(index + 1));
    }

    /// <summary>
    /// Adds an item to the end of the rope.
    /// Performance note: This may attempt to perform a balancing.
    /// </summary>
    /// <param name="items">The item to append to this rope.</param>
    /// <returns>A new rope that is the concatenation of the current sequence and the specified item.</returns>
    [Pure]
    public readonly Rope<T> Add(T item) => new Rope<T>(this, new Rope<T>(item)).Balanced();

    /// <summary>
    /// Concatenates two sequences together into a single sequence.
    /// Performance note: This may attempt to perform a balancing.
    /// </summary>
    /// <param name="items">The items to append to this rope.</param>
    /// <returns>A new rope that is the concatenation of the current sequence and the specified items.</returns>
    [Pure]
    public readonly Rope<T> AddRange(Rope<T> items)
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
    public readonly Rope<T> Slice(long start)
    {
        if (start == 0)
        {
            return this;
        }

        if (start == this.Length)
        {
            return Empty;
        }

        switch (this.data)
        {
            // NOTE: ValueTuple<T> is an impossible case because startIndex would have to be either 0 or 1, if not it's out of range.
            case ReadOnlyMemory<T> m:
                return new Rope<T>(m[(int)start..]);
            case RopeNode node:
                if (start <= this.Weight)
                {
                    var newLeft = node.left.Slice(start);
                    return new Rope<T>(newLeft, node.right);
                }
                else
                {
                    return node.right.Slice(start - this.Weight);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(start));
        }
    }

    /// <summary>
    /// Gets a range of elements in the form of a new instance of <see cref="Rope{T}"/>.
    /// </summary>
    /// <param name="start">The start to select from.</param>
    /// <param name="length">The number of elements to return.</param>
    /// <returns>A new rope instance if the slice does not cover the entire sequence, otherwise returns the original instance.</returns>
    [Pure]
    public readonly Rope<T> Slice(long start, long length)
    {
        if (length == 0)
        {
            return Empty;
        }

        if (start == 0 && length == this.Length)
        {
            return this;
        }

        return this.Slice(start).RemoveRange(length);
    }

    //[Pure]
    //public static bool operator ==(Rope<T> a, Rope<T> b) => Rope<T>.Equals(a, b);

    //[Pure]
    //public static bool operator !=(Rope<T> a, Rope<T> b) => !Rope<T>.Equals(a, b);

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
    public static implicit operator Rope<T>(ReadOnlyMemory<T> a) => new Rope<T>(a);

    [Pure]
    public static implicit operator Rope<T>(T[] a) => new Rope<T>(a);

    [Pure]
    public static implicit operator Rope<T>(string a) =>
        typeof(T) == typeof(char) ? (Rope<T>)(object)new Rope<char>(a.AsMemory()) :
        typeof(T) == typeof(byte) ? (Rope<T>)(object)new Rope<byte>(Encoding.UTF8.GetBytes(a)) :
        typeof(T) == typeof(Rune) ? (Rope<T>)(object)a.EnumerateRunes().ToRope() :
        throw new InvalidCastException("Cannot implicitly convert type {typeof(T).Name} to string");

    /// <summary>
    /// Determines if the rope's binary tree is unbalanced and then recursively rebalances if necessary.
    /// </summary>
    /// <returns>A balanced tree or the original rope if not out of range.</returns>
    [Pure]
    public readonly Rope<T> Balanced()
    {
        // Early return if the tree is already balanced or not a node		
        if (this.IsBalanced)
        {
            return this;
        }

        if (this.data is RopeNode node)
        {
            ///rebalances.Add(1);
            if (this.Length <= MaxLeafLength)
            {
                // If short enough brute force rebalance into a single leaf.
                return new Rope<T>(this.ToMemory());
            }

            // Calculate the depth difference between left and right
            var leftDepth = node.left.Depth;
            var rightDepth = node.right.Depth;
            var depthDiff = rightDepth - leftDepth;

            ////Debug.Assert(depthDiff <= MaxTreeDepth, "This tree is way too deep?");
            if (depthDiff > MaxDepthImbalance)
            {
                // Example: Right is deep (10), left is shallower (6) -> +4 imbalance
                var (newLeftPart, newRightPart) = node.right.SplitAt(node.right.Length / 2);
                Debug.Assert(newLeftPart.Depth < this.Depth && newRightPart.Depth < this.Depth, "Depth not improving");
                return new Rope<T>(new Rope<T>(node.left, newLeftPart).Balanced(), newRightPart.Balanced());
            }
            else if (depthDiff < -MaxDepthImbalance)
            {
                // Example: Left is deep (10), Right is shallower (6) -> -4 imbalance
                var (newLeftPart, newRightPart) = node.left.SplitAt(node.left.Length / 2);
                Debug.Assert(newLeftPart.Depth < this.Depth && newRightPart.Depth < this.Depth, "Depth not improving");
                return new Rope<T>(newLeftPart.Balanced(), new Rope<T>(newRightPart, node.right).Balanced());
            }

            // return this.Chunk(MaxLeafLength).Select(r => r.ToRope()).Combine();

            // Recursively balance if we are already very long.
            var (left, right) = this.SplitAt(this.Length / 2);
            var (newLeft, newRight) = (left.Balanced(), right.Balanced());
            return new Rope<T>(newLeft, newRight);
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
    public readonly long CommonPrefixLength(Rope<T> other)
    {
        if (this.Length == 0 || other.Length == 0)
        {
            return 0;
        }

        if (this.data is ValueTuple<T> value)
        {
            return value.Equals(other[0]) ? 1 : 0;
        }

        if (this.data is ReadOnlyMemory<T> mem && other is ReadOnlyMemory<T> otherMem)
        {
            // Finding a Leaf within another leaf.
            return MemoryExtensions.CommonPrefixLength(mem.Span, otherMem.Span);
        }

        long common = 0;
        var rentedBuffers = BufferPool.Rent(this.BufferCount);
        var rentedFindBuffers = BufferPool.Rent(other.BufferCount);
        try
        {
            var buffers = rentedBuffers[..this.BufferCount];
            var findBuffers = rentedFindBuffers[..other.BufferCount];
            this.FillBuffers(buffers);
            other.FillBuffers(findBuffers);

            var aligned = new AlignedBufferEnumerator<T>(buffers, findBuffers);
            while (aligned.MoveNext())
            {
                var c = aligned.CurrentA.CommonPrefixLength(aligned.CurrentB);
                common += c;
                if (c != aligned.CurrentA.Length)
                {
                    break;
                }
            }
        }
        finally
        {
            BufferPool.Return(rentedFindBuffers);
            BufferPool.Return(rentedBuffers);
        }

        return common;

        // Performance analysis: https://neil.fraser.name/news/2007/10/09/
        // var n = Math.Min(this.Length, other.Length);
        // for (long i = 0; i < n; i++)
        // {
        // 	if (!this.ElementAt(i).Equals(other.ElementAt(i)))
        // 	{
        // 		return i;
        // 	}
        // }

        // return n;
    }

    /// <summary>
    /// Determine the common suffix length of two sequences (The number of elements that are shared at the end of the sequence) between this sequence and another sequence.
    /// </summary>
    /// <param name="other">Other sequence to compare against.</param>
    /// <returns>The number of characters common to the end of each sequence.</returns>
    [Pure]
    public readonly long CommonSuffixLength(Rope<T> other)
    {
        if (this.Length == 0 || other.Length == 0)
        {
            return 0;
        }

        if (this.data is ValueTuple<T> value)
        {
            return value.Item1.Equals(other[^1]) ? 1 : 0;
        }

        if (this.data is ReadOnlyMemory<T> mem && other is ReadOnlyMemory<T> otherMem)
        {
            // Finding a Leaf within another leaf.
            var span = mem.Span;
            var otherSpan = otherMem.Span;

            // Performance analysis: https://neil.fraser.name/news/2007/10/09/
            var nx = Math.Min(span.Length, otherSpan.Length);
            for (var i = 1; i <= nx; i++)
            {
                if (!span[span.Length - i].Equals(otherSpan[otherSpan.Length - i]))
                {
                    return i - 1;
                }
            }

            return nx;
        }


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
    /// Gets the memory representation of this sequence, may allocate a new array if this instance is a tree.
    /// </summary>
    /// <returns>Read only memory of the rope.</returns>
    [Pure]
    public readonly ReadOnlyMemory<T> ToMemory()
    {
        switch (this.data)
        {
            case ValueTuple<T> value:
                return new T[] { value.Item1 }.AsMemory();
            case ReadOnlyMemory<T> memory:
                return memory;
            case RopeNode:
                // Instead of: new T[this.left.Length + this.right.Length]; we use an uninitialized array as we are copying over the entire contents.
                var result = GC.AllocateUninitializedArray<T>((int)this.Length);
                var mem = result.AsMemory();
                this.CopyBuffers(mem);
                return mem;
            default:
                return ReadOnlyMemory<T>.Empty;
        };
    }

    /// <summary>
    /// Copies the rope into a new single contiguous array of elements.
    /// </summary>
    /// <returns>A new array filled with the contents of the sequence.</returns>
    [Pure]
    public readonly T[] ToArray()
    {
        switch (this.data)
        {
            case ValueTuple<T> value:
                return new T[] { value.Item1 };
            case ReadOnlyMemory<T> memory:
                return memory.ToArray();
            case RopeNode:
                // Instead of: new T[this.left.Length + this.right.Length]; we use an uninitialized array as we are copying over the entire contents.
                var result = GC.AllocateUninitializedArray<T>((int)this.Length);
                var mem = result.AsMemory();
                this.CopyBuffers(mem);
                return result;
            default:
                return Array.Empty<T>();
        };
    }

    /// <summary>
    /// Copies the rope into the specified memory buffer.
    /// </summary>
    /// <param name="other">The target to copy to.</param>
    public readonly void CopyTo(Memory<T> other)
    {
        switch (this.data)
        {
            case ValueTuple<T> value:
                other.Span[0] = value.Item1;
                break;
            case ReadOnlyMemory<T> memory:
                // Leaf node so copy memory.
                memory.CopyTo(other);
                break;
            case RopeNode:
                this.CopyBuffers(other);
                break;
        }
    }

    private readonly void CopyBuffers(Memory<T> other)
    {
        var rentedBuffers = BufferPool.Rent(this.BufferCount);
        try
        {
            var buffers = rentedBuffers[..this.BufferCount];
            this.FillBuffers(buffers);
            foreach (var b in buffers)
            {
                b.CopyTo(other[..b.Length]);
                other = other[b.Length..];
            }
        }
        finally
        {
            BufferPool.Return(rentedBuffers);
        }
    }

    [Pure]
    public readonly long IndexOf(Rope<T> find)
    {
        if (find.Length > this.Length)
        {
            return -1;
        }

        if (find.Length == 0)
        {
            return 0;
        }
        else if (this.Length == 0)
        {
            return -1;
        }

        // Finding a Leaf within another leaf.
        switch (this.data)
        {
            case ValueTuple<T> value:
                return find.data is ValueTuple<T> findValue && value.Item1.Equals(findValue.Item1) ? 0 : -1;
            case ReadOnlyMemory<T> mem:
                if (find.data is ReadOnlyMemory<T> findMem)
                {
                    return mem.Span.IndexOf(findMem.Span);
                }
                break;
            default:
                break;
        }

        var rentedBuffers = BufferPool.Rent(this.BufferCount);
        var rentedFindBuffers = BufferPool.Rent(find.BufferCount);
        long index = -1;
        try
        {
            var buffers = rentedBuffers[..this.BufferCount];
            var findBuffers = rentedFindBuffers[..find.BufferCount];
            this.FillBuffers(buffers);
            find.FillBuffers(findBuffers);
            index = this.IndexOfDefaultEquality(buffers, findBuffers);
        }
        finally
        {
            BufferPool.Return(rentedFindBuffers);
            BufferPool.Return(rentedBuffers);
        }

        return index;
    }

    /// <summary>
    /// Finds the index of a subset of the rope
    /// </summary>
    /// <param name="find"></param>
    /// <returns>A number greater than or equal to zero if the sub-sequence is found, otherwise returns -1.</returns>
    [Pure]
    public readonly long IndexOf<TEqualityComparer>(Rope<T> find, TEqualityComparer comparer) where TEqualityComparer : IEqualityComparer<T>
    {
        if (find.Length > this.Length)
        {
            return -1;
        }

        if (find.Length == 0)
        {
            return 0;
        }

        // Attempt 1: Naive split of slow and fast paths.
        //if (!this.IsNode)
        //{
        //	var dataSpan = this.data.Span;
        //	var isDefault = object.ReferenceEquals(EqualityComparer<T>.Default, comparer);
        //	if (!find.IsNode && isDefault)
        //	{
        //		return dataSpan.IndexOf(find.data.Span);
        //	}

        // 	// Check in the 'data' array for a starting match that could spill over to 'right'
        // 	for (var i = 0; i < dataSpan.Length - find.Length; i++)
        //     {
        //         var match = isDefault ? find.StartsWithSpanFast(dataSpan[i..]) : find.StartsWithSpanSlow(comparer, dataSpan[i..]);
        //         if (match)
        //         {
        //             return i;
        //         }
        //     }

        //     return -1;
        // }

        // for (var i = 0; i < this.Length; i++)
        // {
        // 	bool match = true;
        // 	for (var j = 0; j < find.Length && match; j++)
        // 	{
        // 		if (i + j < buffer.Length)
        // 		{
        // 			match = comparer.Equals(buffer[(int)(i + j)], find[j]);
        // 		}
        // 		else
        // 		{
        // 			match = false;
        // 		}
        // 	}

        // 	if (match)
        // 	{
        // 		return i;
        // 	}
        // }

        // // Indicate that no match was found
        // return -1;


        // Attempt 2: Flattened access to buffers and slow search (Big memory allocation overhead).
        var buffers = BufferPool.Rent(this.BufferCount);
        long i = -1;
        try
        {
            this.FillBuffers(buffers);
            var findBuffer = find.ToMemory(); // PERF: This is BAD!!
            i = this.IndexOfSlow(buffers, findBuffer, comparer);
        }
        finally
        {
            BufferPool.Return(buffers);
        }

        return i;


        // Attempt 3: WIP - Shifting through buffers and using IndexOf for fast seek.
        // using var a = this.Buffers.GetEnumerator();
        // using var b = find.Buffers.GetEnumerator();
        // if (a.MoveNext() && b.MoveNext())
        // {
        // 	var aSpan = a.Current.Span;
        // 	var bSpan = b.Current.Span;
        // 	while (true)
        // 	{
        // 		// Jump to the first element to seek to a possible start
        // 		var f = aSpan.IndexOf(bSpan[0]);
        // 		if (f == -1)
        // 		{
        // 			if (a.MoveNext())
        // 			{
        // 				aSpan = a.Current.Span;
        // 			}
        // 			else
        // 			{
        // 				return -1;
        // 			}
        // 		}
        // 		else
        // 		{
        // 			var goodStart = aSpan[f..];

        // 			// Check if F is a good start?
        // 			while (true)
        // 			{
        // 				var common = goodStart.CommonPrefixLength(bSpan);
        // 				if (common == bSpan.Length)
        // 				{
        // 					if (b.MoveNext())
        // 					{
        // 						goodStart = goodStart.Slice(common);

        // 						// Keep going!
        // 						bSpan = b.Current.Span;
        // 					}
        // 					else
        // 					{
        // 						// That was the last span, we're done!
        // 						return f;
        // 					}
        // 				}
        // 				else if (common == aSpan.Length)
        // 				{
        // 					if (a.MoveNext())
        // 					{
        // 						aSpan = a.Current.Span;
        // 					}
        // 					else
        // 					{
        // 						return -1;
        // 					}
        // 				}
        // 			}
        // 		}			
        // 	}
        // }

        // return -1;
    }

    private readonly void FillBuffers(Span<ReadOnlyMemory<T>> buffers)
    {
        if (buffers.Length > 0)
        {
            if (this.data is ValueTuple<T> value)
            {
                buffers[0] = new[] { value.Item1 };
            }
            else if (this.data is ReadOnlyMemory<T> mem)
            {
                buffers[0] = mem;
            }
            else if (this.data is RopeNode node)
            {
                node.left.FillBuffers(buffers[..node.left.BufferCount]);
                node.right.FillBuffers(buffers[node.left.BufferCount..]);
            }
        }
    }

    public readonly void WriteTo(IBufferWriter<T> writer)
    {
        switch (this.data)
        {
            case ValueTuple<T> value:
                var span = writer.GetSpan(1);
                span[0] = value.Item1;
                writer.Advance(1);
                break;
            case ReadOnlyMemory<T> mem:
                writer.Write(mem.Span);
                break;
            case RopeNode node:
                node.left.WriteTo(writer);
                node.right.WriteTo(writer);
                break;
            default:
                break;
        }
    }

    private static int MoveToFirstElement(T element, ref ReadOnlySpan<T> current, ref ReadOnlySpan<ReadOnlyMemory<T>> currentAndRemainder)
    {
        int globalOffset = 0;
        while (true)
        {
            var result = current.IndexOf(element);
            if (result == -1)
            {
                globalOffset += current.Length;
                currentAndRemainder = currentAndRemainder[1..];
                if (currentAndRemainder.Length == 0)
                {
                    current = ReadOnlySpan<T>.Empty;
                    return -1;
                }

                current = currentAndRemainder[0].Span;
            }
            else
            {
                current = current[result..];
                return globalOffset + result;
            }
        }
    }

    private readonly long IndexOfDefaultEquality(ReadOnlySpan<ReadOnlyMemory<T>> targetBuffers, ReadOnlySpan<ReadOnlyMemory<T>> findBuffers)
    {
        // 1. Fast forward to first element in findBuffers
        // 2. Reduce find buffer using sequence equal,
        // 2.1    If no match, slice target buffers
        // 2.2    Else iterate until all sliced sub-sequences matches
        var remainingTargetBuffers = targetBuffers;
        var currentTargetSpan = remainingTargetBuffers[0].Span;
        var firstElement = findBuffers[0].Span[0];
        long globalIndex = 0;
        while (remainingTargetBuffers.Length > 0)
        {
            // Reset find to the beginning
            var remainingFindBuffers = findBuffers;
            var currentFindSpan = remainingFindBuffers[0].Span;

            var index = MoveToFirstElement(firstElement, ref currentTargetSpan, ref remainingTargetBuffers);
            if (index == -1)
            {
                return -1;
            }

            var aligned = new AlignedBufferEnumerator<T>(currentTargetSpan, currentFindSpan, remainingTargetBuffers, remainingFindBuffers);
            var matches = true;
            while (aligned.MoveNext())
            {
                if (!aligned.CurrentA.SequenceEqual(aligned.CurrentB))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                // We matched and had nothing left in B. We're done!
                if (aligned.RemainderB.Length == 0)
                {
                    return globalIndex + index;
                }

                // We matched everything so far in A but B had more to search for, so there was no match.
                return -1;
            }
            else
            {

                // We found an index but it wasn't a match, shift by 1 and do an indexof again.
                if (currentTargetSpan.Length > 0)
                {
                    currentTargetSpan = currentTargetSpan[1..];
                    globalIndex += index + 1;
                }
                else
                {
                    globalIndex += remainingTargetBuffers[0].Length;
                    remainingTargetBuffers = remainingTargetBuffers[1..];
                }
            }
        }

        //    int globalIndex = 0; // Tracks overall position across all buffers
        //    foreach (var targetBuffer in targetBuffers)
        //    {
        //        for (int targetSpanIndex = 0; targetSpanIndex < targetBuffer.Length; targetSpanIndex++)
        //        {
        //            int j = 0;
        //            bool match = true;
        //foreach (var findBuffer in findBuffers)
        //{
        //	var findSpan = findBuffer.Span;
        //	while (findSpan.Length > 0 && match)
        //	{
        //		int globalOffset = globalIndex + j;
        //		ReadOnlySpan<T> range = default;
        //		if (TryGetSpanAtGlobalIndex(targetBuffers, globalOffset, findSpan.Length, ref range))
        //		{
        //			if (range.SequenceEqual(findSpan[..range.Length]))
        //			{
        //				j += range.Length;
        //				findSpan = findSpan[range.Length..];
        //			}
        //			else
        //			{
        //				match = false;
        //				break;
        //			}
        //		}
        //                    else
        //                    {
        //                        match = false;
        //                        break;
        //                    }
        //                }
        //}

        //            if (match)
        //            {
        //                return globalIndex;
        //            }

        //            globalIndex++; // Move to the next global position
        //        }
        //    }

        return -1; // Not found
    }

    private readonly long IndexOfSlow<TEqualityComparer>(ReadOnlySpan<ReadOnlyMemory<T>> buffers, ReadOnlyMemory<T> find, TEqualityComparer comparer) where TEqualityComparer : IEqualityComparer<T>
    {
        long globalIndex = 0; // Tracks overall position across all buffers

        for (int bufIndex = 0; bufIndex < buffers.Length; bufIndex++)
        {
            ReadOnlySpan<T> bufferSpan = buffers[bufIndex].Span;
            for (int i = 0; i < bufferSpan.Length; i++)
            {
                bool match = true;
                var findSpan = find.Span;
                for (int j = 0; j < findSpan.Length && match; j++)
                {
                    long globalOffset = globalIndex + j;
                    if (!TryGetValueAtGlobalIndex(buffers, globalOffset, out var value) || !comparer.Equals(value, findSpan[j]))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return globalIndex;
                }

                globalIndex++; // Move to the next global position
            }
        }

        return -1; // Not found
    }

    /// <summary>
    /// Helper method to get value at a global index across all buffers
    /// </summary>
    /// <param name="buffers">Span of memory buffers</param>
    /// <param name="globalIndex">The global index to find.</param>
    /// <param name="value">The output value</param>
    /// <returns>true if found, otherwise false.</returns>
    private readonly bool TryGetValueAtGlobalIndex(ReadOnlySpan<ReadOnlyMemory<T>> buffers, long globalIndex, out T? value)
    {
        long accumulatedLength = 0;
        foreach (var buffer in buffers)
        {
            if (globalIndex < accumulatedLength + buffer.Length)
            {
                value = buffer.Span[(int)(globalIndex - accumulatedLength)];
                return true;
            }

            accumulatedLength += buffer.Length;
        }

        value = default;
        return false; // Global index out of range
    }

    private readonly bool TryGetSpanAtGlobalIndex(ReadOnlySpan<ReadOnlyMemory<T>> buffers, int globalIndex, int maxLength, ref ReadOnlySpan<T> value)
    {
        int accumulatedLength = 0;
        foreach (var buffer in buffers)
        {
            if (globalIndex < accumulatedLength + buffer.Length)
            {
                value = buffer.Span[(globalIndex - accumulatedLength)..];
                Debug.Assert(value.Length != 0, "Empty span!");
                if (value.Length > maxLength)
                {
                    value = value[..maxLength];
                }

                return true;
            }

            accumulatedLength += buffer.Length;
        }

        value = default;
        return false; // Global index out of range
    }

    // private bool StartsWithSpanFast(ReadOnlySpan<T> dataSpan)
    // {
    // 	if (this.IsNode)
    // 	{
    // 		return this.Left.StartsWithSpanFast(dataSpan) && this.Right.StartsWithSpanFast(dataSpan[this.Left.Count..]);
    // 	}
    // 	else
    // 	{
    // 		return this.data.Span.StartsWith(dataSpan);
    // 	}
    // }

    // private bool StartsWithSpanSlow<TEqualityComparer>(TEqualityComparer comparer, ReadOnlySpan<T> dataSpan) where TEqualityComparer : IEqualityComparer<T>
    // {
    // 	if (this.IsNode)
    // 	{
    // 		return this.Left.StartsWithSpanSlow(comparer, dataSpan[..this.Left.Count]) && this.Right.StartsWithSpanSlow(comparer, dataSpan[this.Left.Count..]);
    // 	}
    // 	else
    // 	{
    // 		var match = true;
    // 		var thisSpan = this.data.Span;
    // 		for (var j = 0; j < thisSpan.Length && match; j++)
    // 		{
    // 			if (j < dataSpan.Length)
    // 			{
    // 				match = comparer.Equals(dataSpan[j], thisSpan[j]);
    // 			}
    // 			else
    // 			{
    // 				match = false;
    // 			}
    // 		}

    // 		return match;
    // 	}
    // }

    [Pure]
    public readonly long IndexOf(Rope<T> find, long offset)
    {
        var i = this.Slice(offset).IndexOf(find);
        if (i != -1)
        {
            return i + offset;
        }

        return -1;
    }

    [Pure]
    public readonly long IndexOf(ReadOnlyMemory<T> find)
    {
        switch (this.data)
        {
            case ValueTuple<T> value:
                return find.Length == 1 && value.Item1.Equals(find.Span[0]) ? 0 : -1;
            case ReadOnlyMemory<T> memory:
                return memory.Span.IndexOf(find.Span);
            case RopeNode:
                // Use the complicated logic.
                return this.IndexOf(new Rope<T>(find));
            default:
                return -1;
        }
    }

    [Pure]
    public readonly long IndexOf(T find)
    {
        switch (this.data)
        {
            case ValueTuple<T> value:
                return value.Item1.Equals(find) ? 0 : -1;
            case ReadOnlyMemory<T> memory:
                return memory.Span.IndexOf(find);
            case RopeNode node:
                // Node
                var i = node.left.IndexOf(find);
                if (i != -1)
                {
                    return i;
                }

                i = node.right.IndexOf(find);
                if (i != -1)
                {
                    return node.left.Length + i;
                }

                return -1;
            default:
                return -1;
        }
    }

    [Pure]
    public readonly long IndexOf(T find, long offset)
    {
        var i = this.Slice(offset).IndexOf(find);
        if (i != -1)
        {
            return i + offset;
        }

        return -1;
    }

    [Pure]
    public readonly long IndexOf(ReadOnlyMemory<T> find, long offset)
    {
        var i = this.Slice(offset).IndexOf(find);
        if (i != -1)
        {
            return i + offset;
        }

        return -1;
    }

    [Pure]
    public readonly bool StartsWith(Rope<T> find) => this.Slice(0, Math.Min(this.Length, find.Length)).IndexOf(find) == 0;

    [Pure]
    public readonly bool StartsWith(ReadOnlyMemory<T> find) => this.StartsWith(new Rope<T>(find));


    public readonly long LastIndexOf(Rope<T> find)
    {
        if (find.Length == 0)
        {
            // Adjust the return value to conform with .NET's behavior.
            // return the length of 'this' as the next plausible index.
            return this.Length;
        }

        if (this.data is ValueTuple<T> value && find is ValueTuple<T> findValue)
        {
            return value.Item1.Equals(findValue.Item1) ? 0 : -1;
        }

        if (this.data is ReadOnlyMemory<T> mem && find is ReadOnlyMemory<T> findMem)
        {
            // Finding a Leaf within another leaf.
            return mem.Span.LastIndexOf(findMem.Span);
        }

        var rentedBuffers = BufferPool.Rent(this.BufferCount);
        var rentedFindBuffers = BufferPool.Rent(find.BufferCount);
        var buffers = rentedBuffers[..this.BufferCount];
        var findBuffers = rentedFindBuffers[..find.BufferCount];
        this.FillBuffers(buffers);
        find.FillBuffers(findBuffers);

        var i = LastIndexOfDefaultEquality(buffers, findBuffers, find.Length);

        BufferPool.Return(rentedFindBuffers);
        BufferPool.Return(rentedBuffers);
        return i;
    }

    /// <summary>
    /// Returns the last element index that matches the specified sub-sequence, working backwards from the startIndex (inclusive).
    /// </summary>
    /// <param name="find">The sequence to find, if empty will return the startIndex + 1.</param>
    /// <param name="startIndex">The starting index to start searching backwards from (Optional).</param>
    /// <returns>The last element index that matches the sub-sequence, skipping the offset elements.</returns>
    [Pure]
    public readonly long LastIndexOf(Rope<T> find, long startIndex) => this.Slice(0, Math.Min(startIndex + 1, this.Length)).LastIndexOf(find);

    private readonly long LastIndexOfSlow(Rope<T> find, long startIndex)
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

    private static long MoveToLastElement(long length, T element, ref ReadOnlySpan<T> current, ref ReadOnlySpan<ReadOnlyMemory<T>> currentAndRemainder)
    {
        var globalOffset = length;
        while (true)
        {
            var result = current.LastIndexOf(element);
            if (result == -1)
            {
                globalOffset -= current.Length;
                currentAndRemainder = currentAndRemainder[..^1];
                if (currentAndRemainder.Length == 0)
                {
                    current = ReadOnlySpan<T>.Empty;
                    return -1;
                }

                current = currentAndRemainder[^1].Span;
            }
            else
            {
                var index = globalOffset - (current.Length - result);
                current = current[..(result + 1)];
                return index;
            }
        }
    }

    private readonly long LastIndexOfDefaultEquality(ReadOnlySpan<ReadOnlyMemory<T>> targetBuffers, ReadOnlySpan<ReadOnlyMemory<T>> findBuffers, long findLength)
    {
        // 1. Fast forward to last element in findBuffers
        // 2. Reduce find buffer using sequence equal,
        // 2.1    If no match, slice target buffers
        // 2.2    Else iterate until all sliced sub-sequences matches
        var remainingTargetBuffers = targetBuffers;
        var currentTargetSpan = remainingTargetBuffers[^1].Span;
        var lastElement = findBuffers[^1].Span[^1];
        var endIndex = this.Length;
        while (remainingTargetBuffers.Length > 0)
        {
            // Reset find to the beginning
            var remainingFindBuffers = findBuffers;
            var currentFindSpan = remainingFindBuffers[^1].Span;

            var index = MoveToLastElement(endIndex, lastElement, ref currentTargetSpan, ref remainingTargetBuffers);
            if (index == -1)
            {
                return -1;
            }

            var aligned = new ReverseAlignedBufferEnumerator<T>(currentTargetSpan, currentFindSpan, remainingTargetBuffers, remainingFindBuffers);
            var matches = true;
            while (aligned.MoveNext())
            {
                if (!aligned.CurrentA.SequenceEqual(aligned.CurrentB))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                // We matched and had nothing left in B. We're done!
                if (!aligned.HasRemainderB)
                {
                    return index + 1 - findLength;
                }

                // We matched everything so far in A but B had more to search for, so there was no match.
                return -1;
            }
            else
            {
                // We found an index but it wasn't a match, shift by 1 and do an indexof again.
                if (currentTargetSpan.Length > 0)
                {
                    endIndex = index;
                    currentTargetSpan = currentTargetSpan[..^1];
                }
                else
                {
                    remainingTargetBuffers = remainingTargetBuffers[..^1];
                }
            }
        }

        return -1;
    }

    [Pure]
    public readonly long LastIndexOf(T find, int startIndex) => this.Slice(0, startIndex + 1).LastIndexOf(new Rope<T>(find));

    [Pure]
    public readonly long LastIndexOf(T find) => this.LastIndexOf(new Rope<T>(find));

    [Pure]
    public readonly bool EndsWith(Rope<T> find)
    {
        var i = this.LastIndexOf(find);
        return i != -1 && i == this.Length - find.Length;
    }

    [Pure]
    public readonly bool EndsWith(ReadOnlyMemory<T> find) => this.EndsWith(new Rope<T>(find));

    /// <summary>
    /// Replaces all occurrences of the specified element with it's specified replacement.
    /// </summary>
    /// <param name="replace">The element to find.</param>
    /// <param name="with">The element to replace with.</param>
    /// <returns>A new rope with the replacements made.</returns>
    [Pure]
    public readonly Rope<T> Replace(T replace, T with)
    {
        return this.Replace(new Rope<T>(replace), new Rope<T>(with));
    }

    [Pure]
    public readonly Rope<T> Replace(Rope<T> replace, Rope<T> with)
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
    public readonly Rope<T> Replace<TEqualityComparer>(Rope<T> replace, Rope<T> with, TEqualityComparer comparer) where TEqualityComparer : IEqualityComparer<T>
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
    /// Converts this to a string representation. 
    /// If <typeparamref name="T"/> is <see cref="char"/> a string of the contents is returned.
    /// If <typeparamref name="T"/> is <see cref="byte"/> a UTF8 encoded string of the contents is returned.
    /// </summary>
    /// <returns>A string representation of the sequence.</returns>
    [Pure]
    public readonly override string ToString()
    {
        switch (this)
        {
            case Rope<char> chars:
                return new string(chars.ToMemory().Span);
            case Rope<byte> utf8:
                return Encoding.UTF8.GetString(utf8.ToMemory().Span);
            case Rope<Rune> runes:
                return string.Concat(this.Select(rune => rune.ToString()));
            default:
                return string.Join("\n", this.Select(element => element.ToString()));
        }
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
    public readonly Rope<T> InsertSorted<TComparer>(T item, TComparer comparer) where TComparer : IComparer<T>
    {
        var index = this.BinarySearch(item, comparer);
        if (index < 0)
        {
            index = ~index;
        }

        var (left, right) = this.SplitAt(index);
        // TODO: Common sorted prefix?
        var insert = new Rope<T>(item);
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
    public readonly long BinarySearch<TComparer>(long index, int count, T item, IComparer<T> comparer) where TComparer : IComparer<T>
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
    public readonly long BinarySearch<TComparer>(T item, TComparer comparer) where TComparer : IComparer<T>
    {
        switch (this.data)
        {
            case ValueTuple<T> value:
                return value.Item1.Equals(item) ? 0 : -1;
            case ReadOnlyMemory<T> memory:
                return MemoryExtensions.BinarySearch(memory.Span, item, comparer);
            case RopeNode node:
                var r = node.right.BinarySearch(item, comparer);
                if (r != -1)
                {
                    return node.left.Length + r;
                }

                var l = node.left.BinarySearch(item, comparer);
                return l;
            default:
                return -1;
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
    public readonly long BinarySearch(T item)
    {
        switch (this.data)
        {
            case ValueTuple<T> value:
                return value.Item1.Equals(item) ? 0 : -1;
            case ReadOnlyMemory<T> memory:
                return MemoryExtensions.BinarySearch(memory.Span, item, Comparer<T>.Default);
            case RopeNode node:
                var r = node.right.BinarySearch(item, Comparer<T>.Default);
                if (r != -1)
                {
                    return node.left.Length + r;
                }

                var l = node.left.BinarySearch(item, Comparer<T>.Default);
                return l;
            default:
                return -1;
        }
    }

    /// <summary>
    /// Gets a value indicating whether these two ropes are equivalent in terms of their content.
    /// </summary>
    /// <param name="other">The other sequence to compare to</param>
    /// <returns>true if both instances hold the same sequence, otherwise false.</returns>
    [Pure]
    public readonly bool Equals(Rope<T> other)
    {
        return this.data switch
        {
            ReadOnlyMemory<T> mem =>
                other.data switch
                {
                    ReadOnlyMemory<T> otherMem => mem.Span.SequenceEqual(otherMem.Span),
                    _ => AlignedEquals(other)
                },
            ValueTuple<T> value => other.data is ValueTuple<T> otherValue && value.Item1.Equals(otherValue.Item1),
            _ => AlignedEquals(other)
        };
    }

    private bool AlignedEquals(Rope<T> other)
    {
        if (this.Length != other.Length)
        {
            return false;
        }

        if (this.Length == 0)
        {
            // Both must be empty if lengths are equal.
            return true;
        }

        var rentedBuffers = BufferPool.Rent(this.BufferCount);
        var rentedFindBuffers = BufferPool.Rent(other.BufferCount);
        var buffers = rentedBuffers[..this.BufferCount];
        var findBuffers = rentedFindBuffers[..other.BufferCount];
        this.FillBuffers(buffers);
        other.FillBuffers(findBuffers);
        var aligned = new AlignedBufferEnumerator<T>(buffers, findBuffers);
        var matches = true;
        while (aligned.MoveNext())
        {
            if (!aligned.CurrentA.SequenceEqual(aligned.CurrentB))
            {
                matches = false;
                break;
            }
        }

        BufferPool.Return(rentedFindBuffers);
        BufferPool.Return(rentedBuffers);
        return matches;
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
        > 64 => HashCode.Combine(this[0], this[(int)(this.Length * 0.25)], this[(int)(this.Length * 0.75)], this[this.Length - 1], this.Length),
        > 3 => HashCode.Combine(this[0], this[this.Length / 2], this[this.Length - 1], this.Length),
        > 2 => HashCode.Combine(this[0], this[this.Length - 1], this.Length),
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
    private static bool CalculateIsBalanced(long length, int depth) => depth < DepthToFibonnaciPlusTwo.Length && length >= DepthToFibonnaciPlusTwo[depth];

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
    /// Efficiently copies a List into a single leaf node.
    /// </summary>
    /// <param name="items">Read only list of items to copy from</param>
    /// <returns>A new rope or <see cref="Empty"/> if items is empty.</returns>
    [Pure]
    public static Rope<T> FromList(List<T> list)
    {
        if (list.Count == 0)
        {
            return Rope<T>.Empty;
        }

        ReadOnlySpan<T> span = CollectionsMarshal.AsSpan(list);
        return Rope<T>.FromReadOnlySpan(ref span);
    }

    /// <summary>
    /// Copies a read only list into a perfectly balanced binary tree.
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

        if (items.Count == 1)
        {
            return new Rope<T>(items[0]);
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

        if (span.Length == 1)
        {
            return new Rope<T>(span[0]);
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

        return new Rope<T>(memory);
    }

    /// <summary>
    /// Alias for <see cref="Rope<T>.Empty"/>.
    /// </summary>
    /// <returns>An empty rope instance.</returns>
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
            // TODO: Improve this enumerators performance, should use a buffer enumerator pattern.
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