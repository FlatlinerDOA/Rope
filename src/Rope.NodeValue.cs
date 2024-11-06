namespace Rope;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

public readonly partial record struct Rope<T> where T : IEquatable<T>
{
    /// <summary>
    /// Stores two ropes together.
    /// </summary>
    /// <param name="Left">The left part of the B-Tree.</param>
    /// <param name="Right">The right part of the B-Tree.</param>
    private readonly record struct NodeValue(Rope<T> Left, Rope<T> Right, byte Depth, bool IsBalanced, int Count) : IRopeData<T>
    {
        public readonly T this[int index] => this.Left.Length <= index ? this.Right[index - this.Left.Length] : this.Left[index];

        public readonly long Length => this.Left.Length + this.Right.Length;

        public readonly int BufferCount => this.Left.BufferCount + this.Right.BufferCount;

        public readonly T ElementAt(long index) => this.Left.Length <= index ? this.Right.ElementAt(index - this.Left.Length) : this.Left.ElementAt(index);

        public readonly long IndexOf(T find)
        {
            // Node
            var i = Left.IndexOf(find);
            if (i != -1)
            {
                return i;
            }

            i = Right.IndexOf(find);
            if (i != -1)
            {
                return Left.Length + i;
            }

            return -1;
        }

        public readonly Rope<T> Slice(long start)
        {
            if (start <= this.Left.Length)
            {
                var newLeft = this.Left.Slice(start);
                return new Rope<T>(newLeft, this.Right);
            }
            else
            {
                return this.Right.Slice(start - this.Left.Length);
            }
        }

        public readonly Rope<T> RemoveRange(long startIndex)
        {
            if (startIndex <= this.Left.Length)
            {
                return this.Left.RemoveRange(startIndex);
            }
            else
            {
                return new Rope<T>(this.Left, this.Right.RemoveRange(startIndex - this.Left.Length));
            }
        }

        public readonly IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public (Rope<T> Left, Rope<T> Right) SplitAt(long i, int recursionRemaining)
        {
            if (i == 0)
            {
                return (Empty, new Rope<T>(this.Left, this.Right));
            }
            else if (i == this.Length)
            {
                return (new Rope<T>(this.Left, this.Right), Empty);
            }
            else if (i <= this.Left.Length)
            {
                var (newLeft, newRight) = this.Left.SplitAt(i, --recursionRemaining);
                return (newLeft, new Rope<T>(newRight, this.Right));
            }
            else
            {
                var (a, b) = this.Right.SplitAt(i - this.Left.Length, --recursionRemaining);
                return (new Rope<T>(this.Left, a), b);
            }
        }
    }
}
