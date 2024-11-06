using System;
using System.Collections.Generic;
using System.Text;

namespace Rope
{
    internal interface IRopeData<T> : IReadOnlyList<T> where T : IEquatable<T>
    {
        long LongLength => this.Count;

        Rope<T> Slice(long start);

        T ElementAt(long index);

        Rope<T> RemoveRange(long startIndex);

        ////(Rope<T> Left, Rope<T> Right) SplitAt(long i, int recursionRemaining);

        ////long CommonPrefixLength(Rope<T> other);
        ////long CommonSuffixLength(Rope<T> other);

        ////long IndexOf(T find);

        ////long IndexOf(Rope<T> find);

        ////long IndexOf<TEqualityComparer>(Rope<T> find, TEqualityComparer comparer) where TEqualityComparer : IEqualityComparer<T>;

        ////long LastIndexOf(T find);

        ////long LastIndexOf(Rope<T> find);

        ////long LastIndexOf<TEqualityComparer>(T find, TEqualityComparer equalityComparer) where TEqualityComparer : IEqualityComparer<T>;

        ////void FillBuffers(Span<ReadOnlyMemory<T>> buffers);
    }
}
