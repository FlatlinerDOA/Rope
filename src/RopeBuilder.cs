namespace Rope;

using System;

internal static class RopeBuilder
{
    internal static Rope<T> Create<T>(ReadOnlySpan<T> values) where T : IEquatable<T> => Rope<T>.FromReadOnlySpan(ref values);
}
