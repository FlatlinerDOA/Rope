using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class CreateNewWithLength10
{
    private char[] array = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

    [Benchmark(Description = "string.ToRope()")]
    public void RopeOfCharFromString() => _ = "0123456789".ToRope();

    [Benchmark(Description = "new Rope<char>(array)")]
    public void RopeOfCharFromArray() => _ = new Rope<char>(this.array);

    [Benchmark(Description = "new List<char>(array)")]
    public void ListOfCharFromArray() => _ = new List<char>(this.array);

    [Benchmark(Description = "new StringBuilder(string)")]
    public void StringBuilder() => _ = new StringBuilder("0123456789");
}