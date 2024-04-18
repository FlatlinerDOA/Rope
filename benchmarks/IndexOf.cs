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

    [Benchmark]
    public void RopeOfChar()
    {
        _ = BenchmarkData.LongDiffText1.IndexOf(Find);
    }

    [Benchmark]
    public void RopeOfCharFill()
    {
        _ = BenchmarkData.LongDiffText1.IndexOfFill(Find);
    }

    [Benchmark]
    public void FragmentedRopeOfChar()
    {
        _ = FragmentedRope.IndexOf(Find);
    }

    [Benchmark]
    public void FragmentedRopeOfCharFill()
    {
        _ = FragmentedRope.IndexOfFill(Find);
    }

    [Benchmark]
    public void FragmentedRopeOfCharLarge()
    {
        _ = FragmentedRope.IndexOf(FragmentedFind);
    }

    [Benchmark]
    public void FragmentedRopeOfCharFillLarge()
    {
        _ = FragmentedRope.IndexOfFill(FragmentedFind);
    }

    [Benchmark]
    public void String()
    {
        _ = BenchmarkData.LongDiffText1String.IndexOf("[[New Haven Register]]");
    }
}