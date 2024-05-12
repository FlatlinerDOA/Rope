namespace Benchmarks;

using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using Rope;

[MemoryDiagnoser]
public class IndexOf
{
    private static readonly Rope<char> FragmentedRope = BenchmarkData.LongDiffText1.Chunk(256).Select(c => c.ToRope()).Combine();

    private static readonly Rope<char> FragmentedFind = FragmentedRope[(int)(FragmentedRope.Length * 0.33)..(int)(FragmentedRope.Length * 0.66)];

    private static readonly Rope<char> Find = "[[New Haven Register]]".ToRope();

    [Params(10, 100, 1000, 10000)]
    public int Length;

    [Benchmark(Description = "Rope")]
    public void RopeOfChar()
    {
        _ = BenchmarkData.LongDiffText1[..this.Length].IndexOf(Find);
    }

    [BenchmarkCategory("Fragmented\nRope")]
    [Benchmark(Description = "IndexOf")]
    public void FragmentedRopeOfChar()
    {
        _ = FragmentedRope[..this.Length].IndexOf(Find);
    }

    [BenchmarkCategory("Fragmented\nRope")]
    [Benchmark(Description = "IndexOf (Fragmented Find)")]
    public void FragmentedRopeOfCharLarge()
    {
        _ = FragmentedRope[..this.Length].IndexOf(FragmentedFind);
    }

    [BenchmarkCategory("string")]
    [Benchmark]
    public void String()
    {
        _ = BenchmarkData.LongDiffText1String[..this.Length].IndexOf("[[New Haven Register]]");
    }
}