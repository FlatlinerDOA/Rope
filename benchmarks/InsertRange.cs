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

    [Benchmark]
    public void RopeOfChar()
    {
        var lorem = BenchmarkData.LoremIpsum.ToRope();
        var s = lorem;
        for (int i = 0; i < EditCount; i++)
        {
            s = s.InsertRange(321, lorem);
        }

        ////s.ToString();
    }

    [Benchmark]
    public void ListOfChar()
    {
        var lorem = BenchmarkData.LoremIpsum.ToCharArray();
        var s = new List<char>(lorem);
        for (int i = 0; i < EditCount; i++)
        {
            s.InsertRange(321, lorem);
        }

        ////s.ToString();
    }

    [Benchmark]
    public void StringBuilder()
    {
        var s = new StringBuilder(BenchmarkData.LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s.Insert(321, BenchmarkData.LoremIpsum);
        }

        ////s.ToString();
    }
}
