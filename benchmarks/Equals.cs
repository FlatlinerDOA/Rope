using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class Equals
{
    [Params(10, 100, 1000, 10000)]
    public int Length;

    [Benchmark(Description = "Rope<char>")]
    public void RopeOfChar()
    {
        _ = BenchmarkData.LoremIpsum.ToRope().Slice(Length).Equals(BenchmarkData.LoremIpsum.ToRope().Slice(Length));
    }

    [Benchmark(Description = "StringBuilder")]
    public void StringBuilder()
    {
        var s = new StringBuilder(BenchmarkData.LoremIpsum[..Length]);
        _ = s.Equals(BenchmarkData.LoremIpsum[..Length].AsSpan());
    }

    [Benchmark(Description = "string")]
    public void String()
    {
        var s = BenchmarkData.LoremIpsum[..Length];
        _ = s.Equals(BenchmarkData.LoremIpsum[..Length]);
    }


    [Benchmark(Description = "ImmutableArray<char>")]
    public void ImmutableArrayOfChar()
    {
        var s = ImmutableArray<char>.Empty.AddRange(BenchmarkData.LoremIpsum[..Length].AsSpan());
        _ = s.Equals(BenchmarkData.LoremIpsum[..Length]);
    }
}
