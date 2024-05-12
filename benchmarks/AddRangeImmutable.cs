using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class AddRangeImmutable
{
    [Params(10, 100)]
    public int EditCount;

    [Benchmark(Description = "Rope")]
    public void RopeOfChar()
    {
        var lorem = BenchmarkData.LoremIpsum.ToRope();
        var s = lorem;
        for (int i = 0; i < EditCount; i++)
        {
            s = s.AddRange(lorem);
        }
    }

    [Benchmark(Description = "ImmutableList\n.Builder")]
    public void ImmutableListBuilderOfChar()
    {
        var s = ImmutableList<char>.Empty.ToBuilder();
        s.AddRange(BenchmarkData.LoremIpsum);

        for (int i = 0; i < EditCount; i++)
        {
            s.AddRange(BenchmarkData.LoremIpsum);
        }
    }

    [Benchmark(Description = "ImmutableList")]
    public void ImmutableListOfChar()
    {
        var s = ImmutableList<char>.Empty.AddRange(BenchmarkData.LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s = s.AddRange(BenchmarkData.LoremIpsum);
        }
    }

    [Benchmark(Description = "ImmutableArray")]
    public void ImmutableArrayOfChar()
    {
        var s = ImmutableArray<char>.Empty.AddRange(BenchmarkData.LoremIpsum.AsSpan());
        for (int i = 0; i < EditCount; i++)
        {
            s = s.AddRange(BenchmarkData.LoremIpsum.AsSpan());
        }
    }
}