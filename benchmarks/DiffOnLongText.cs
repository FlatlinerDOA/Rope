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
        var dmp = new DiffMatchPatch();
        dmp.DiffOptions = dmp.DiffOptions with { TimeoutSeconds = 0 };
        _ = dmp.CalculateDifferences(BenchmarkData.LongDiffText1, BenchmarkData.LongDiffText2);
    }
}