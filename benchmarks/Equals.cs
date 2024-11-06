using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

/// <summary>
/// This benchmark performs a successful equality and then a negative equality of various lengths of text.
/// </summary>
[MemoryDiagnoser]
public class Equals
{
    [Params(1, 2, 3, 10, 100, 1000, 10000)]
    public int Length;

    private Rope<char> ropeX;
    private Rope<char> ropeY;
    private Rope<char> ropeZ;

    private StringBuilder? sbX;
    private ReadOnlyMemory<char> sbY;
    private ReadOnlyMemory<char> sbZ;

    private string? strX;
    private string? strY;
    private string? strZ;

    [GlobalSetup]
    public void Setup()
    {
        this.ropeX = BenchmarkData.LoremIpsum.ToRope().Slice(Length);
        this.ropeY = BenchmarkData.LoremIpsum.ToRope().Slice(Length);
        this.ropeZ = BenchmarkData.LoremIpsum.ToRope().Slice(1, Length - 1);
        this.sbX = new StringBuilder(BenchmarkData.LoremIpsum[..Length]);
        this.sbY = BenchmarkData.LoremIpsum[..Length].AsMemory();
        this.sbZ = BenchmarkData.LoremIpsum[1..(Length - 1)].AsMemory();
        this.strX = BenchmarkData.LoremIpsum[..Length];
        this.strY = BenchmarkData.LoremIpsum[..Length];
        this.strZ = BenchmarkData.LoremIpsum[1..(Length - 1)];
    }

    [Benchmark(Description = "Rope<char>", Baseline = true)]
    public void RopeOfChar()
    {
        _ = this.ropeX.Equals(this.ropeY);
        _ = this.ropeX.Equals(this.ropeZ);
    }

    [Benchmark(Description = "StringBuilder")]
    public void StringBuilder()
    {
        _ = this.sbX!.Equals(this.sbY.Span);
        _ = this.sbX!.Equals(this.sbZ.Span);
    }

    [Benchmark(Description = "string")]
    public void String()
    {
        _ = this.strX!.Equals(strY);
        _ = this.strX!.Equals(strZ);
    }
}
