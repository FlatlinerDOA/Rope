using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class AddRange
{
    [Params(10, 100, 1000)]
    public int EditCount;

    [Benchmark(Description = "Rope<char>")]
    public void RopeOfChar()
    {
        var lorem = BenchmarkData.LoremIpsum.ToRope();
        var s = lorem;
        for (int i = 0; i < EditCount; i++)
        {
            s = s.AddRange(lorem);
        }

        ////s.ToString();
    }

    [Benchmark(Description = "StringBuilder")]
    public void StringBuilder()
    {
        var s = new StringBuilder(BenchmarkData.LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s.Append(BenchmarkData.LoremIpsum);
        }

        ////s.ToString();
    }

    [Benchmark(Description = "List<char>")]
    public void ListOfChar()
    {
        var s = new List<char>(BenchmarkData.LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s.AddRange(BenchmarkData.LoremIpsum);
        }

        ////s.ToString();
    }

    [Benchmark(Description = "ImmutableList<char>\n.Builder")]
    public void ImmutableListBuilderOfChar()
    {
        var s = ImmutableList<char>.Empty.ToBuilder();
        s.AddRange(BenchmarkData.LoremIpsum);
        
        for (int i = 0; i < EditCount; i++)
        {
            s.AddRange(BenchmarkData.LoremIpsum);
        }

        ////s.ToString();
    }

    [Benchmark(Description = "ImmutableList<char>")]
    public void ImmutableListOfChar()
    {
        var s = ImmutableList<char>.Empty.AddRange(BenchmarkData.LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s = s.AddRange(BenchmarkData.LoremIpsum);
        }

        ////s.ToString();
    }

    [Benchmark(Description = "ImmutableArray<char>")]
    public void ImmutableArrayOfChar()
    {
        var s = ImmutableArray<char>.Empty.AddRange(BenchmarkData.LoremIpsum.AsSpan());
        for (int i = 0; i < EditCount; i++)
        {
            s = s.AddRange(BenchmarkData.LoremIpsum.AsSpan());
        }

        ////s.ToString();
    }
}
