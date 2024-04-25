// Copyright 2010 Google Inc.
// All Right Reserved.

using BenchmarkDotNet.Attributes;
using DiffMatchPatch;
using Rope;
using Rope.Compare;

namespace Benchmarks;

[MemoryDiagnoser]
public class DiffOnLongText
{
    [Benchmark]
    public void RopeOfCharDiff()
    {
        var options = DiffOptions<char>.LineLevel with { TimeoutSeconds = 0 };
        _ = BenchmarkData.LongDiffText1.Diff(BenchmarkData.LongDiffText2, options);
    }

    [Benchmark]
    public void DiffMatchPatchDiff()
    {
        var diff = new diff_match_patch();
        diff.Diff_Timeout = 0;
        diff.diff_main(BenchmarkData.LongDiffText1String, BenchmarkData.LongDiffText2String);
    }
}