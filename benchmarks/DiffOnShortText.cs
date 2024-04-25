// Copyright 2010 Google Inc.
// All Right Reserved.

using System;
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
        _ = BenchmarkData.ShortDiffText1.Diff(BenchmarkData.ShortDiffText2);
    }
}
