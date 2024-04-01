# Rope

[![Build Status](https://github.com/FlatlinerDOA/Rope/actions/workflows/dotnet.yml/badge.svg)](https://github.com/FlatlinerDOA/Rope/actions)
[![License](https://img.shields.io/github/license/FlatlinerDOA/Rope.svg)](https://github.com/FlatlinerDOA/Rope/LICENSE)


[![NuGet](https://img.shields.io/nuget/v/FlatlinerDOA.Rope.svg)](https://www.nuget.org/packages/FlatlinerDOA.Rope)
[![downloads](https://img.shields.io/nuget/dt/FlatlinerDOA.Rope)](https://www.nuget.org/packages/FlatlinerDOA.Rope)
![Size](https://img.shields.io/github/repo-size/FlatlinerDOA/Rope.svg) 

C# implementation of a Rope&lt;T&gt; immutable data structure. See the paper [Ropes: an Alternative to Strings: h.-j. boehm, r. atkinson and m. plass](https://www.cs.rit.edu/usr/local/pub/jeh/courses/QUARTERS/FP/Labs/CedarRope/rope-paper.pdf)

A Rope is an immutable sequence built using a b-tree style data structure that is useful for efficiently applying and storing edits, most commonly used with strings, but any list or sequence can be efficiently edited using the Rope data structure.

Where a b-tree has every node in the tree storing a single entry, a rope contains arrays of elements and only subdivides on the edits. The data structure then decides at edit time whether it is optimal to rebalance the tree using the following heuristic:
A rope of depth n is considered balanced if its length is at least Fn+2.

**Note:** This implementation of Rope&lt;T&gt; has a hard-coded upper-bound depth of 46 added to the heuristic from the paper. As this seemed to be optimal for my workloads, your mileage may vary.

## Example Usage
```csharp

// Converting to a Rope<char> doesn't allocate any strings (simply points to the original memory).
Rope<char> text = "My favourite text".ToRope();

// With Rope<T>, splits don't allocate any new strings either.
IEnumerable<Rope<char>> words = text.Split(' '); 

// Calling ToString() allocates a new string at the time of conversion.
Console.WriteLine(words.First().ToString()); 

// Warning: Concatenating a string to a Rope<char> converts to a string (allocating memory).
string text2 = text + " My second favourite text";

// Better: This makes a new rope out of the other two ropes, no string allocations.
Rope<char> text3 = text + " My second favourite text".ToRope();

```

## Comparison with StringBuilder
A comparison could be drawn between a Rope and a StringBuilder as they use a very similar technique for efficient edits.

|Feature|Rope&lt;T&gt;|StringBuilder|
|-------|-------------|-------------|
|Efficient edits of strings (avoid double allocations)| ✅ |✅|
|Supports items of any type| ✅ |❌|
|Immutable edits| ✅ |❌|
|Thread safe| ✅ |❌|
|Copy free insertion| ✅ |✅|
|Copy free splitting| ✅ |❌|
|UTF8 strings| ✅ |❌|


### Performance and Memory Allocation Comparison

Working with a string of length - 32644 characters. - MaxLeafLength = 64kb, Max Depth = 46

| Method                   | IterationCount | Mean             | Error           | StdDev          | Median           | Gen0       | Gen1       | Gen2       | Allocated   |
|------------------------- |--------------- |-----------------:|----------------:|----------------:|-----------------:|-----------:|-----------:|-----------:|------------:|
| StringBuilderAppend      | 10             |      35,464.3 ns |     1,236.52 ns |     3,607.00 ns |      34,138.1 ns |    42.9688 |    31.1890 |          - |    721160 B |
| RopeAppend               | 10             |         909.4 ns |         4.22 ns |         3.74 ns |         909.2 ns |     0.0362 |          - |          - |       616 B |
| StringBuilderInsert      | 10             |      23,586.3 ns |       569.68 ns |     1,661.79 ns |      23,441.0 ns |    42.9688 |    27.3438 |          - |    721160 B |
| RopeInsert               | 10             |       1,912.5 ns |        19.74 ns |        18.46 ns |       1,915.3 ns |     0.1659 |          - |          - |      2800 B |
| StringBuilderSplitConcat | 10             |      23,259.0 ns |       765.85 ns |     2,246.11 ns |      22,986.0 ns |    23.4680 |    11.7188 |          - |    393720 B |
| RopeSplitConcat          | 10             |         627.5 ns |        12.46 ns |        21.16 ns |         625.0 ns |     0.2003 |          - |          - |      3360 B |
| StringBuilderAppend      | 100            |     424,026.9 ns |    15,058.49 ns |    43,926.31 ns |     420,691.4 ns |   394.5313 |   382.3242 |          - |   6621560 B |
| RopeAppend               | 100            |   3,788,876.6 ns |    69,073.37 ns |    64,611.27 ns |   3,795,896.9 ns |   496.0938 |   496.0938 |   496.0938 |  12644474 B |
| StringBuilderInsert      | 100            |     402,212.6 ns |    11,030.96 ns |    32,002.81 ns |     399,662.2 ns |   394.5313 |   378.9063 |          - |   6621560 B |
| RopeInsert               | 100            |     153,401.2 ns |       922.11 ns |       862.54 ns |     153,175.8 ns |     1.4648 |          - |          - |     28000 B |
| StringBuilderSplitConcat | 100            |     182,440.9 ns |     3,226.08 ns |     2,859.84 ns |     182,958.8 ns |   199.9512 |    99.8535 |          - |   3347160 B |
| RopeSplitConcat          | 100            |       5,903.0 ns |        34.10 ns |        31.89 ns |       5,903.3 ns |     2.0065 |          - |          - |     33600 B |
| StringBuilderAppend      | 1000           |  23,812,058.1 ns |   463,404.49 ns |   799,349.98 ns |  23,832,889.1 ns |  5437.5000 |  5406.2500 |  1625.0000 |  65637636 B |
| RopeAppend               | 1000           | 167,322,273.8 ns | 1,923,747.06 ns | 1,705,351.66 ns | 167,967,016.7 ns | 17666.6667 | 17666.6667 | 17666.6667 | 870805045 B |
| StringBuilderInsert      | 1000           |  25,097,010.5 ns |   501,208.08 ns | 1,454,095.52 ns |  25,410,140.6 ns |  5437.5000 |  5406.2500 |  1625.0000 |  65627392 B |
| RopeInsert               | 1000           |  14,747,672.0 ns |    31,837.03 ns |    29,780.37 ns |  14,743,734.4 ns |    15.6250 |          - |          - |    280006 B |
| StringBuilderSplitConcat | 1000           |   1,413,954.7 ns |    14,469.64 ns |    11,296.95 ns |   1,411,454.4 ns |  1962.8906 |   494.1406 |   125.0000 |  32881603 B |
| RopeSplitConcat          | 1000           |      59,257.5 ns |       813.76 ns |       721.38 ns |      59,193.0 ns |    20.0806 |          - |          - |    336000 B |