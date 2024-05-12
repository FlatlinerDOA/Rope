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

    private Rope<char> ropeX;
    private Rope<char> ropeY;
    private StringBuilder sbX;
    private ReadOnlyMemory<char> sbY;

    private string strX;
    private string strY;

    private ImmutableArray<char> arrayX;

    private ImmutableArray<char> arrayY;

    [GlobalSetup]
    public void Setup()
    {
        this.ropeX = BenchmarkData.LoremIpsum.ToRope().Slice(Length);
        this.ropeY = BenchmarkData.LoremIpsum.ToRope().Slice(Length);
        this.sbY = BenchmarkData.LoremIpsum[..Length].AsMemory();
        this.sbX = new StringBuilder(BenchmarkData.LoremIpsum[..Length]);
        this.strX = BenchmarkData.LoremIpsum[..Length];
        this.strY = BenchmarkData.LoremIpsum[..Length];
        this.arrayX =  ImmutableArray<char>.Empty.AddRange(BenchmarkData.LoremIpsum[..Length].AsSpan());
        this.arrayY = ImmutableArray<char>.Empty.AddRange(BenchmarkData.LoremIpsum[..Length].AsSpan());
    }

    [Benchmark(Description = "Rope<char>")]
    public void RopeOfChar()
    {
        _ = this.ropeX.Equals(this.ropeY);
    }

    [Benchmark(Description = "StringBuilder")]
    public void StringBuilder()
    {        
        _ = this.sbX.Equals(this.sbY.Span);
    }

    [Benchmark(Description = "string")]
    public void String()
    {
        _ = this.strX.Equals(strY);
    }
}
