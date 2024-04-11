using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class AppendRange
{
    [Params(10, 100, 1000)]
    public int EditCount;

    [Benchmark(Description = "Rope<char> += ")]
    public void RopeOfChar()
    {
        var lorem = BenchmarkData.LoremIpsum.ToRope();
        var s = lorem;
        for (int i = 0; i < EditCount; i++)
        {
            s += lorem;
        }

        ////s.ToString();
    }

    [Benchmark(Description = "List<char>.AddRange")]
    public void ListOfChar()
    {
        var s = new List<char>(BenchmarkData.LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s.AddRange(BenchmarkData.LoremIpsum);
        }

        ////s.ToString();
    }

    [Benchmark(Description = "StringBuilder.Append")]
    public void StringBuilder()
    {
        var s = new StringBuilder(BenchmarkData.LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s.Append(BenchmarkData.LoremIpsum);
        }

        ////s.ToString();
    }
}
