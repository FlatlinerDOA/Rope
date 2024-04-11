using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class InsertSorted
{
    private static readonly Random random = new(42);
    
    private static readonly long[] RandomLongs = Enumerable.Range(0, 65000).Select(s => random.NextInt64(32000)).ToArray();

    private static readonly float[] RandomFloats = Enumerable.Range(0, 65000).Select(s => random.NextSingle()).ToArray();

    [Benchmark]
    public void RopeOfLong() 
    {
        var rope = Rope<long>.Empty;
        var comparer = Comparer<long>.Default;
        for (int i = 0; i < RandomLongs.Length; i++)
        {
            rope = rope.InsertSorted(RandomLongs[i], comparer);
        }
    }

    [Benchmark]
    public void PriorityQueueOfLong() 
    {
        var queue = new PriorityQueue<long, long>();
        for (int i = 0; i < RandomLongs.Length; i++)
        {
            queue.Enqueue(i, RandomLongs[i]);
        }
    }

    [Benchmark]
    public void RopeOfFloat() 
    {
        var rope = Rope<float>.Empty;
        var comparer = Comparer<float>.Default;
        for (int i = 0; i < RandomFloats.Length; i++)
        {
            rope = rope.InsertSorted(RandomFloats[i], comparer);
        }
    }

    [Benchmark]
    public void PriorityQueueOfFloat() 
    {
        var queue = new PriorityQueue<long, float>();
        for (int i = 0; i < RandomFloats.Length; i++)
        {
            queue.Enqueue(i, RandomFloats[i]);
        }
    }
}