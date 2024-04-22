namespace Rope;

using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmarks;

[MemoryDiagnoser]
public class IndexOf
{ 
    private static readonly Rope<char> FragmentedRope = BenchmarkData.LongDiffText1.Chunk(256).Select(c => c.ToRope()).Combine();

    private static readonly Rope<char> FragmentedFind = FragmentedRope[(int)(FragmentedRope.Length  * 0.33)..(int)(FragmentedRope.Length  * 0.66)];

    private static readonly Rope<char> Find = "[[New Haven Register]]".ToRope();

    [Benchmark(Description = "Rope<char>")]
    public void RopeOfChar()
    {
        _ = BenchmarkData.LongDiffText1.IndexOf(Find);
    }

    [BenchmarkCategory("Fragmented\nRope<char>")]
    [Benchmark(Description = "IndexOf")]
    public void FragmentedRopeOfChar()
    {
        _ = FragmentedRope.IndexOf(Find);
    }

    [BenchmarkCategory("Fragmented\nRope<char>")]
    [Benchmark(Description = "IndexOf (Fragmented Find)")]
    public void FragmentedRopeOfCharLarge()
    {
        _ = FragmentedRope.IndexOf(FragmentedFind);
    }

    [BenchmarkCategory("string")]
    [Benchmark]
    public void String()
    {
        _ = BenchmarkData.LongDiffText1String.IndexOf("[[New Haven Register]]");
    }
}