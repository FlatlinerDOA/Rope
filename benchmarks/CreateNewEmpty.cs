using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class CreateNewEmpty
{
    [Benchmark(Description = "Rope<char>.Empty", Baseline = true)]
    public void EmptyRopeOfChar() => _ = Rope<char>.Empty;

    [Benchmark(Description = "new List<char>()")]
    public void EmptyListOfChar() => _ = new List<char>();

    [Benchmark(Description = "new StringBuilder()")]
    public void EmptyStringBuilder() => _ = new StringBuilder();
}
