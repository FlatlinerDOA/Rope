namespace Rope;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public readonly partial record struct Rope<T> where T : IEquatable<T>
{
    /// <summary>
    /// sizeof(EmptyValue) is 1 byte.
    /// </summary>
    /// <param name="Value"></param>
    private readonly record struct EmptyValue() : IRopeData<T>
    {
        public static readonly EmptyValue Empty = new();

        public readonly T this[int index] => throw new IndexOutOfRangeException();

        public readonly int Count => 0;

        public T ElementAt(long index) => throw new IndexOutOfRangeException();

        public readonly IEnumerator<T> GetEnumerator() => new EmptyEnumerator();

        public Rope<T> RemoveRange(long startIndex) => startIndex == 0 ?
            Rope<T>.Empty :
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        public readonly Rope<T> Slice(long start) => start == 0 ? Rope<T>.Empty : throw new ArgumentOutOfRangeException(nameof(start));

        public readonly (Rope<T> Left, Rope<T> Right) SplitAt(long i, int recursionRemaining) => i == 0 ?
            (Rope<T>.Empty, Rope<T>.Empty) :
            throw new ArgumentOutOfRangeException(nameof(i));

        IEnumerator IEnumerable.GetEnumerator() => new EmptyEnumerator();

        private readonly struct EmptyEnumerator() : IEnumerator<T>
        {
            public T Current => throw new ArgumentOutOfRangeException();

            object IEnumerator.Current => throw new ArgumentOutOfRangeException();

            public void Dispose()
            {
            }

            public bool MoveNext() => false;

            public void Reset()
            {
            }
        }
    }
}
