using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class InsertRange
{
    [Params(10, 100, 1000)]
    public int EditCount;

    [Benchmark(Baseline = true)]
    public Rope<char> RopeOfChar()
    {
        var lorem = BenchmarkData.LoremIpsum.ToRope();
        var s = lorem;
        for (int i = 0; i < EditCount; i++)
        {
            s = s.InsertRange(321, lorem);
        }

        return s;
    }

    [Benchmark]
    public List<char> ListOfChar()
    {
        var lorem = BenchmarkData.LoremIpsum.ToCharArray();
        var s = new List<char>(lorem);
        for (int i = 0; i < EditCount; i++)
        {
            s.InsertRange(321, lorem);
        }

        return s;
    }

    [Benchmark]
    public StringBuilder StringBuilder()
    {
        var s = new StringBuilder(BenchmarkData.LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s.Insert(321, BenchmarkData.LoremIpsum);
        }

        return s;
    }
}
