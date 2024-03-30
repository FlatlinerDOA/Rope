
namespace Rope;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// A rope is an immutable sequence built using a b-tree style data structure that is useful for efficiently applying and storing edits, most commonly to text, but any list or sequence can be edited.
/// </summary>
public sealed record Rope<T> : IEnumerable<T> where T : IEquatable<T>
{
	public const int MaxLeafLength = 1024;

	public static readonly Rope<T> Empty = new Rope<T>();

	private static readonly double phi = (1 + Math.Sqrt(5)) / 2;

	private static readonly double phiDiv = (2 * phi - 1);

	private static readonly int[] DepthToFibonnaciPlusTwo = Enumerable.Range(0, 46).Select(d => Fibonnaci(d) + 2).ToArray();	

	/// <summary>Left slice of the raw buffer</summary>
	private readonly Rope<T>? left;

	/// <summary>Right slice of the raw buffer (null if a leaf node.)</summary>
	private readonly Rope<T>? right;
	
	/// <summary>Data is the raw underlying buffer, or a cached copy of the ToMemory call (for efficiency).</summary>
	private readonly ReadOnlyMemory<T> data;

	public Rope()
	{
		this.data = ReadOnlyMemory<T>.Empty;
	}

	public Rope(ReadOnlyMemory<T> data)
	{
		if (data.Length <= MaxLeafLength)
		{
			// This is a leaf
			this.data = data;
		}
		else
		{
			// This is a binary tree node
			var half = (int)data.Length / 2;
			this.left = new Rope<T>(data.Slice(0, half));
			this.right = new Rope<T>(data.Slice(half, data.Length - half));
			this.data = data;
		}
	}

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
			if (!left.IsNode)
			{
				this.data = left.data;
			}
			else
			{
				this.left = left.left;
				this.right = left.right;			
			}
		}
		else if (left.Length == 0)
		{
			if (!right.IsNode)
			{
				this.data = right.data;
			}
			else
			{
				this.left = right.left;
				this.right = right.right;
			}
		}
		else
		{
			this.left = left;
			this.right = right;
			this.data = ReadOnlyMemory<T>.Empty;
		}
	}
	
	public T this[int index] => this.ElementAt(index);

	public Rope<T> Left => this.left;

	public Rope<T> Right => this.right;

	public bool IsNode => this.left != null;

	public int Weight => this.left?.Length ?? this.data.Length;

	public int Length => this.IsNode ? this.left.Length + this.right.Length : this.data.Length;

	public int Depth => this.IsNode ? Math.Max(this.left.Depth, this.right.Depth) + 1 : 0;

	public T ElementAt(int index)
	{
		if (this.left == null) 
		{
			return this.data.Slice(index).Span[0];
		}
		
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

	public IEnumerable<Rope<T>> Split(T separator)
	{
		Rope<T> remainder = this;
		int i = 0;
		do
		{
			i = remainder.IndexOf(separator, i);
			
			if (i != -1)
			{
				yield return remainder.Slice(0, i);
				remainder = remainder.Slice(i + 1);
			}
			else
			{
				yield return remainder;
			}
		}
		while (i != -1);
	}

	public IEnumerable<Rope<T>> Split(ReadOnlyMemory<T> separator)
	{
		Rope<T> remainder = this;
		int i = 0;
		do
		{
			i = remainder.IndexOf(separator, i);

			if (i != -1)
			{
				yield return remainder.Slice(0, i);
				remainder = remainder.Slice(i + separator.Length);
			}
			else
			{
				yield return remainder;
			}
		}
		while (i != -1);
	}

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
				var (a, b) = this.left.SplitAt(i);
				return (a, new Rope<T>(b, this.right));
			}
			else
			{
				var (a, b) = this.right.SplitAt(i - this.Weight);
				return (new Rope<T>(this.left, a), b);
			}
		}

		return (new Rope<T>(this.data.Slice(0, i)), new Rope<T>(this.data.Slice(i)));
	}

	public Rope<T> Insert(int i, ReadOnlyMemory<T> s)
	{
		return this.Insert(i, new Rope<T>(s));
	}

	public Rope<T> Insert(int i, Rope<T> s)
	{
		if (i > this.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(i));
		}

		var (left, right) = this.SplitAt(i);
		return new Rope<T>(left, new Rope<T>(s, right));
	}

	public Rope<T> Remove(int start, int length)
	{
		return new Rope<T>(this.Slice(0, start), this.Slice(start + length));
	}

	public Rope<T> Remove(int start)
	{
		return this.Slice(start, this.Length - start);
	}

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

		return new Rope<T>(this, source).Balanced();
	}

	public Rope<T> Slice(int start)
	{
		return this.Slice(start, this.Length - start);
	}

	public Rope<T> Slice(int start, int length)
	{
		var (head, _) = this.SplitAt(start + length);
		return head.SplitAt(start).Right;
	}

	public static Rope<T> operator +(Rope<T> a, Rope<T> b) => a.Concat(b);
	public static Rope<T> operator +(Rope<T> a, T b) => a.Concat(new Rope<T>(new T[] { b }.AsMemory()));

	public static implicit operator Rope<T> (ReadOnlyMemory<T> a) => new Rope<T>(a);
	public static implicit operator Rope<T> (ReadOnlySpan<T> a) => new Rope<T>(a.ToArray());
	public static implicit operator Rope<T> (T[] a) => new Rope<T>(a);

	public Rope<T> Balanced()
	{
		// https://www.cs.rit.edu/usr/local/pub/jeh/courses/QUARTERS/FP/Labs/CedarRope/rope-paper.pdf
		// | p. 1319 - We define the depth of a leaf to be 0, and the depth of a concatenation to be
		// | one plus the maximum depth of its children. Let Fn be the nth Fibonacci number.
		// | A rope of depth n is balanced if its length is at least Fn+2, e.g. a balanced rope
		// | of depth 1 must have length at least 2.
		var balanced = this.Depth <= 46 && this.Length > DepthToFibonnaciPlusTwo[this.Depth];
		if (!balanced)
		{
			// Brute force rebalance, could be done faster.
			return new Rope<T>(this.ToMemory());
		}

		return this;
	}

	private static int Fibonnaci(int n)
	{
		if (n > 46)
		{
			// Overflow
			return int.MaxValue;
		}

		// Binet's Formula - https://en.wikipedia.org/wiki/Fibonacci_number#Closed-form_expression
		return (int)((Math.Pow(phi, n) - Math.Pow(-phi, -n)) / phiDiv);
	}
	
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
	/// Determine the common suffix of two strings.
	/// </summary>
	/// <param name="other">other text to compare against.</param>
	/// <returns>The number of characters common to the end of each string.</returns>
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

	public T[] ToArray()
	{
		if (this.left == null)
		{
			return this.data.ToArray();
		}

		var sb = new T[this.left.Length + this.right.Length];
		this.left.CopyTo(sb.AsMemory());
		this.right.CopyTo(sb.AsMemory().Slice(this.left.Length));
		return sb;
	}
	
	public void CopyTo(Memory<T> other)
	{
		if (this.left == null)
		{
			// Leaf node so copy memory.
			this.data.CopyTo(other);
		}
		else
		{
			// Binary tree so copy each half.
			this.left.CopyTo(other);
			this.right.CopyTo(other.Slice(this.left.Length));
		}
	}
	
	public int IndexOf(Rope<T> find)
	{
		return this.IndexOf(find.ToMemory());
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
		if (this.left == null)
		{
			// Leaf
			return this.data.Span.IndexOf(find.Span);
		}
		else
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
		
		return -1;
	}

	public int IndexOf(T find)
	{
		if (this.left == null)
		{
			// Leaf
			return this.data.Span.IndexOf(find);
		}
		else
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

	public bool StartsWith(Rope<T> find)
	{
		return this.IndexOf(find) == 0;
	}

	public bool StartsWith(ReadOnlyMemory<T> find) => this.StartsWith(new Rope<T>(find));

	public int LastIndexOf(Rope<T> find, int offset = 0)
	{
		if (find.Length > this.Length - offset)
		{
			return -1;
		}

		if (this.IsNode || find.IsNode)
		{
			var fend = find.Length - 1; // End position of find.
			var matched = false;
			for (int i = (this.Length - 1) - offset; i >= 0; i--)
			{
				if (find[fend].Equals(this[i]))
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

	public override string ToString()
	{
		if (this is Rope<char> rc)
		{
			return new string(rc.ToMemory().Span);
		}
		
		return typeof(T).Name + " x " + this.Depth;
	}

	public ReadOnlyMemory<T> ToMemory()
	{
		if (this.left == null)
		{
			return this.data;
		}
		
		return this.ToArray().AsMemory();
	}

	public bool Equals(Rope<T>? other)
	{
		if (ReferenceEquals(other, null))
		{
			return false;
		}
		
		if (this.GetHashCode() != other.GetHashCode()) 
		{
			return false;
		}
		
		if (this.left == null && other.left == null)
		{
			return this.data.Span.IndexOf(other.data.Span) == 0 && this.data.Length == other.data.Length;
		}
		else
		{
			for (int i = 0; i < this.Length; i++)
			{
				if (!this.ElementAt(i).Equals(other.ElementAt(i)))
				{
					return false;
				}
			}
		}
		
		return true;
	}
	
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
		private Rope<T> rope;
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
			this.index = -1;
			this.rope = null;
		}

		public bool MoveNext()
		{
			if (this.rope == null)
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
