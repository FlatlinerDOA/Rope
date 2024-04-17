// Copyright 2010 Google Inc.
// All Right Reserved.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Rope;
using Rope.Compare;

namespace Benchmarks;

[MemoryDiagnoser]
public class DiffOnShortText
{
    [Benchmark]
    public void DiffMain()
    {
        var dmp = new DiffMatchPatch();
        _ = dmp.diff_main(BenchmarkData.ShortDiffText1, BenchmarkData.ShortDiffText2);
    }  
}
