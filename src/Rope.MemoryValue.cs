namespace Rope;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public readonly partial record struct Rope<T> where T : IEquatable<T>
{
    private readonly record struct MemoryValue(ReadOnlyMemory<T> Data) : IRopeData<T>
    {
        public readonly int Count => this.Data.Length;

        public readonly T this[int index] => this.Data.Span[index];

        public readonly T ElementAt(long index) => this.Data.Span[(int)index];

        public readonly Rope<T> Slice(long start) => new Rope<T>(this.Data.Slice((int)start));

        public readonly Rope<T> RemoveRange(long startIndex) => new Rope<T>(this.Data[..(int)startIndex]);

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

    }
}
