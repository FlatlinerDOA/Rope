#if NET8_0_OR_GREATER
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

    [Benchmark(Baseline = true)]
    public long AddRangeThenOrderLong()
    {
        var rope = Rope<long>.Empty.AddRange(RandomLongs);
        var comparer = Comparer<long>.Default;
        return rope.Order(comparer).First();
    }

    [Benchmark]
    public long InsertSortedLong()
    {
        var rope = Rope<long>.Empty;
        var comparer = Comparer<long>.Default;
        for (int i = 0; i < RandomLongs.Length; i++)
        {
            rope = rope.InsertSorted(RandomLongs[i], comparer);
        }

        return rope[0];
    }

    [Benchmark]
    public long PriorityQueueOfLong()
    {
        var comparer = Comparer<long>.Default;
        var queue = new PriorityQueue<long, long>(comparer);
        for (int i = 0; i < RandomLongs.Length; i++)
        {
            queue.Enqueue(i, RandomLongs[i]);
        }

        return queue.Dequeue();
    }

    [Benchmark]
    public float AddRangeThenOrderFloat()
    {
        var rope = Rope<float>.Empty.AddRange(RandomFloats);
        var comparer = Comparer<float>.Default;
        return rope.Order(comparer).First();
    }

    [Benchmark]
    public float InsertSortedFloat()
    {
        var rope = Rope<float>.Empty;
        var comparer = Comparer<float>.Default;
        for (int i = 0; i < RandomFloats.Length; i++)
        {
            rope = rope.InsertSorted(RandomFloats[i], comparer);
        }

        return rope[0];
    }

    [Benchmark]
    public float PriorityQueueOfFloat()
    {
        var comparer = Comparer<float>.Default;
        var queue = new PriorityQueue<long, float>(comparer);
        for (int i = 0; i < RandomFloats.Length; i++)
        {
            queue.Enqueue(i, RandomFloats[i]);
        }

        return queue.Dequeue();
    }
}
#endif