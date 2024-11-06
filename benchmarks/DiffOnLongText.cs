// Copyright 2010 Google Inc.
// All Right Reserved.
#if NET8_0_OR_GREATER

using BenchmarkDotNet.Attributes;
using DiffMatchPatch;
using Rope;
using Rope.Compare;

namespace Benchmarks;

[MemoryDiagnoser]
public class DiffOnLongText
{
    DiffOptions<char> options = DiffOptions<char>.LineLevel with { TimeoutSeconds = 0 };
    diff_match_patch diff = new diff_match_patch() { Diff_Timeout = 0 };

    [Benchmark(Baseline = true)]
    public void RopeOfCharDiff()
    {
        _ = BenchmarkData.LongDiffText1.Diff(BenchmarkData.LongDiffText2, options);
    }

    [Benchmark]
    public void DiffMatchPatchDiff()
    {
        diff.diff_main(BenchmarkData.LongDiffText1String, BenchmarkData.LongDiffText2String);
    }
}
#endif