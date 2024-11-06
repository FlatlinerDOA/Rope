namespace Rope;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public readonly partial record struct Rope<T> where T : IEquatable<T>
{
    /// <summary>
    /// sizeof(TwoValue) where T is char is 4 bytes, where a char[] is 8 bytes and ReadOnlyMemory is 16 bytes.
    /// </summary>
    /// <param name="Item1">First value</param>
    /// <param name="Item2">Second value</param>
    private readonly record struct TwoValue(T Item1, T Item2) : IRopeData<T>
    {
        public readonly T this[int index] => index == 0 ? this.Item1 : index == 1 ? this.Item2 : throw new ArgumentOutOfRangeException();

        public readonly int Count => 2;

        public readonly T ElementAt(long index) => this[(int)index];

        public readonly IEnumerator<T> GetEnumerator() => new TwoEnumerator(this);

        public Rope<T> RemoveRange(long startIndex) => 
            startIndex == 1 ? new Rope<T>(this.Item1) : 
            startIndex == 0 ? Rope<T>.Empty:
            startIndex == 2 ? new Rope<T>(this.Item1, this.Item2) :
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        public readonly Rope<T> Slice(long start) =>
            start == 1 ? new Rope<T>(this.Item2) :
            start == 0 ? new Rope<T>(this.Item1, this.Item2) :
            throw new ArgumentOutOfRangeException(nameof(start));

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private struct TwoEnumerator : IEnumerator<T>
        {
            private readonly TwoValue value;
            private int index;

            public TwoEnumerator(TwoValue value)
            {
                this.value = value;
                this.index = -1;
            }

            public T Current => index switch
            {
                0 => this.value.Item1,
                1 => this.value.Item2,
                _ => throw new InvalidOperationException()
            };

            object IEnumerator.Current => this.Current;

            public void Dispose() { }

            public bool MoveNext() => ++this.index < 2;

            public void Reset()
            {
                this.index = -1;
            }
        }
    }
}
