using BenchmarkDotNet.Attributes;
using Rope;
using Rope.Compare;
using System;

namespace Benchmarks;

[MemoryDiagnoser]
public class DiffLineHashing
{
    [Benchmark]
    public void LinesToCharsPure()
    {
        // More than 65536 to verify any 16-bit limitation.
        var lineList = Rope<char>.Empty;
        for (int i = 0; i < 66000; i++)
        {
            lineList = lineList.AddRange((i + "\n").AsMemory());
        }

        lineList = lineList.ToMemory();
        var result = lineList.DiffChunksToChars(Rope<char>.Empty, DiffOptions<char>.LineLevel);
    }
}
