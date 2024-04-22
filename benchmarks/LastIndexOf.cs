namespace Rope;

using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmarks;

[MemoryDiagnoser]
public class LastIndexOf
{ 
    private static readonly Rope<char> FragmentedRope = BenchmarkData.LongDiffText1.Chunk(256).Select(c => c.ToRope()).Combine();

    private static readonly Rope<char> FragmentedFind = FragmentedRope[(int)(FragmentedRope.Length  * 0.33)..(int)(FragmentedRope.Length  * 0.66)];

    private static readonly Rope<char> Find = "[[New Haven Register]]".ToRope();

    [Benchmark(Description = "Rope<char>")]
    public void RopeOfChar()
    {
        _ = BenchmarkData.LongDiffText1.LastIndexOf(Find);
    }

    [Benchmark(Description = "Fragmented\nRope<char>")]
    public void FragmentedRopeOfChar()
    {
        _ = FragmentedRope.LastIndexOf(Find);
    }

    [Benchmark(Description = "Fragmented\nRope<char> (Fragmented Find)")]
    public void FragmentedRopeOfCharLarge()
    {
        _ = FragmentedRope.LastIndexOf(FragmentedFind);
    }

    [Benchmark]
    public void String()
    {
        _ = BenchmarkData.LongDiffText1String.LastIndexOf("[[New Haven Register]]");
    }
}