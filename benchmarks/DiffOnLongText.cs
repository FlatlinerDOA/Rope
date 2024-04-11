// Copyright 2010 Google Inc.
// All Right Reserved.

using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class DiffOnLongText
{
    [Benchmark]
    public void DiffMain()
    {
        diff_match_patch dmp = new diff_match_patch();
        dmp.Diff_Timeout = 0;
        dmp.diff_main(BenchmarkData.LongDiffText1, BenchmarkData.LongDiffText2);
    }
}