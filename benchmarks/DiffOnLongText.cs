// Copyright 2010 Google Inc.
// All Right Reserved.

using BenchmarkDotNet.Attributes;
using Rope;
using Rope.Compare;

namespace Benchmarks;

[MemoryDiagnoser]
public class DiffOnLongText
{
    [Benchmark]
    public void SpeedTest()
    {
        _ = BenchmarkData.LongDiffText1.Diff(BenchmarkData.LongDiffText2, DiffOptions<char>.LineLevel with { TimeoutSeconds = 0 });
    }
}