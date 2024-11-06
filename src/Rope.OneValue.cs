namespace Rope;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public readonly partial record struct Rope<T> where T : IEquatable<T>
{
    /// <summary>
    /// sizeof(OneValue) where T is char is 2 bytes, where a char[] is 8 bytes.
    /// </summary>
    /// <param name="Item1"></param>
    private readonly record struct OneValue(T Item1) : IRopeData<T>
    {
        public readonly T this[int index] => index == 0 ? this.Item1 : throw new IndexOutOfRangeException();

        public readonly int Count => 1;

        public T ElementAt(long index) => this[(int)index];

        public readonly IEnumerator<T> GetEnumerator() => new OneEnumerator(this.Item1);

        public Rope<T> RemoveRange(long startIndex) => startIndex == 1 ?
            new Rope<T>(this.Item1) :
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        public readonly Rope<T> Slice(long start) => start == 0 ?
            new Rope<T>(this.Item1) :
            throw new ArgumentOutOfRangeException(nameof(start));

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private struct OneEnumerator : IEnumerator<T>
        {
            private T value;
            private int index;

            public OneEnumerator(T value)
            {
                this.value = value;
                this.index = -1;
            }
            public T Current => this.index == 0 ? this.value : throw new InvalidOperationException();

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
                this.index = -1;
            }

            public bool MoveNext() => ++this.index == 0;

            public void Reset()
            {
                this.index = -1;
            }
        }
    }
}

