// Copyright 2010 Google Inc.
// All Right Reserved.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class DiffOnShortText
{
    [Benchmark]
    public void DiffMain()
    {
        diff_match_patch dmp = new diff_match_patch();
        dmp.diff_main(BenchmarkData.ShortDiffText1, BenchmarkData.ShortDiffText2);
    }  
}
