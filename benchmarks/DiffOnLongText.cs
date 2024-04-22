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
        dmp.Diff_Timeout = 0;
        _ = dmp.diff_main(BenchmarkData.LongDiffText1, BenchmarkData.LongDiffText2);
    }
}