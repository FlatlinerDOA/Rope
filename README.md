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

// Better: This makes a new rope out of the other two ropes, no string allocations or copies.
Rope<char> text3 = text + " My second favourite text".ToRope();

// Value-like equivalence.
"test".ToRope() == ("te".ToRope() + "st".ToRope());
"test".ToRope().GetHashCode() == ("te".ToRope() + "st".ToRope()).GetHashCode();

```

## Comparison with .NET Built in Types
A comparison could be drawn between a Rope and a StringBuilder as they use a very similar technique for efficient edits. List{T} is included as a commonly used alternative.

|Feature|Rope&lt;T&gt;|StringBuilder|List{T}|
|-------|-------------|-------------|-------|
|Supports items of any type| ✅ |❌|✅|
|Immutable edits| ✅ |❌|❌|
|Thread safe| ✅ |❌|❌|
|Copy free Append (avoid double allocations)| ✅ |✅|❌|
|Copy free Insert| ✅ |✅|❌|
|Copy free Remove| ✅ |❌|❌|
|Copy free split| ✅ |❌|❌|
|GC Friendly (No LOH, stays in Gen 0)| ✅ |❌|❌|
|Value-like (Structural invariant GetHashCode and Equals)| ✅ |❌|❌|
|More than 2 billion elements|✅ |❌|❌|


### Performance and Memory Allocation Comparison with StringBuilder
Working with a string of length - 32644 characters. - MaxLeafLength = ~32kb, Max Depth = 46

| Method                            | EditCount | Mean              | Error           | StdDev            | Gen0      | Gen1      | Gen2      | Allocated  |
|---------------------------------- |---------- |------------------:|----------------:|------------------:|----------:|----------:|----------:|-----------:|
| StringBuilderConstructionOverhead | 10        |          6.287 ns |       0.0286 ns |         0.0267 ns |    0.0062 |         - |         - |      104 B |
| **RopeConstructionOverhead**          | 10        |          **3.148 ns** |       0.0574 ns |         0.0509 ns |    0.0033 |         - |         - |       56 B |
| StringBuilderAppend               | 10        |     18,435.239 ns |     155.9500 ns |       138.2456 ns |   42.9688 |   31.2195 |         - |   721160 B |
| **RopeAppend**                        | 10        |        **320.256 ns** |       1.5778 ns |         1.4759 ns |    0.0367 |         - |         - |      616 B |
| StringBuilderInsert               | 10        |     17,713.417 ns |     135.1035 ns |       119.7658 ns |   42.9688 |   27.3438 |         - |   721160 B |
| **RopeInsert**                        | 10        |     **1,029.164 ns** |       4.6957 ns |         3.9211 ns |    0.1659 |         - |         - |     2800 B |
| StringBuilderSplitConcat          | 10        |     17,309.279 ns |     174.0315 ns |       145.3242 ns |   23.4680 |   11.7188 |         - |   393720 B |
| **RopeSplitConcat**                   | 10        |        **497.060 ns** |       5.6149 ns |         5.2522 ns |    0.2003 |         - |         - |     3360 B |
| StringBuilderAppend               | 100       |    248,518.084 ns |   3,957.1789 ns |     3,507.9361 ns |  394.5313 |  382.5684 |         - |  6621560 B |
| **RopeAppend**                        | 100       |    **101,534.844 ns** |     522.2283 ns |       488.4927 ns |    1.7090 |         - |         - |    28616 B |
| StringBuilderInsert               | 100       |    266,237.113 ns |   2,442.1090 ns |     1,906.6381 ns |  394.5313 |  378.9063 |         - |  6621560 B |
| **RopeInsert**                        | 100       |     **60,986.382 ns** |     618.3227 ns |       578.3795 ns |    1.5869 |         - |         - |    28000 B |
| StringBuilderSplitConcat          | 100       |    150,194.484 ns |     853.9897 ns |       713.1201 ns |  199.9512 |   99.8535 |         - |  3347160 B |
| **RopeSplitConcat**                   | 100       |      **4,954.985 ns** |      42.5025 ns |        39.7569 ns |    2.0065 |         - |         - |    33600 B |
| **StringBuilderAppend**               | 1000      | **23,253,005.762 ns** | 455,215.5801 ns |   708,715.8160 ns | 5437.5000 | 5406.2500 | 1625.0000 | 65637897 B |
| RopeAppend                        | 1000      | 36,889,792.381 ns |  63,924.1700 ns |    59,794.7083 ns |   71.4286 |         - |         - |  1630469 B |
| StringBuilderInsert               | 1000      | 23,970,615.375 ns | 504,814.8015 ns | 1,488,457.8320 ns | 5437.5000 | 5406.2500 | 1625.0000 | 65627392 B |
| **RopeInsert**                        | 1000      |  **6,974,877.344 ns** |  35,621.1530 ns |    33,320.0486 ns |   15.6250 |         - |         - |   280003 B |
| StringBuilderSplitConcat          | 1000      |  1,370,860.798 ns |   9,566.2053 ns |     8,480.1918 ns | 1962.8906 |  494.1406 |  125.0000 | 32881603 B |
| **RopeSplitConcat**                   | 1000      |     **49,529.921 ns** |     305.8306 ns |       286.0741 ns |   20.0806 |         - |         - |   336000 B |


### Performance and Memory Allocation Comparison with List&lt;T&gt;

| Method                   | EditCount | Mean                 | Error              | StdDev             | Median               | Gen0      | Gen1      | Gen2      | Allocated   |
|------------------------- |---------- |---------------------:|-------------------:|-------------------:|---------------------:|----------:|----------:|----------:|------------:|
| **ListConstructionOverhead** | 10        |             **2.055 ns** |          0.0171 ns |          0.0152 ns |             2.053 ns |    0.0019 |         - |         - |        32 B |
| RopeConstructionOverhead | 10        |             4.251 ns |          0.0569 ns |          0.0532 ns |             4.247 ns |    0.0033 |         - |         - |        56 B |
| ListAppend               | 10        |       639,685.241 ns |     12,731.1602 ns |     36,116.1824 ns |       657,253.125 ns |  499.0234 |  499.0234 |  499.0234 |   2036400 B |
| **RopeAppend**               | 10        |           **314.925 ns** |          1.9199 ns |          1.7959 ns |           315.212 ns |    0.0367 |         - |         - |       616 B |
| ListInsert               | 10        |       697,516.366 ns |     13,947.9572 ns |     33,951.2555 ns |       710,541.309 ns |  499.0234 |  499.0234 |  499.0234 |   2036400 B |
| **RopeInsert**               | 10        |         **1,035.850 ns** |          5.2297 ns |          4.8919 ns |         1,036.755 ns |    0.1659 |         - |         - |      2800 B |
| ListSplitConcat          | 10        |        40,894.141 ns |        808.1061 ns |      1,393.9433 ns |        40,832.120 ns |   41.6260 |   41.6260 |   41.6260 |    197134 B |
| **RopeSplitConcat**          | 10        |           **495.887 ns** |          3.2931 ns |          3.0803 ns |           496.075 ns |    0.2003 |         - |         - |      3360 B |
| ListAppend               | 100       |     4,327,239.819 ns |     86,425.0487 ns |    200,303.1922 ns |     4,335,406.250 ns |  742.1875 |  742.1875 |  742.1875 |  16748870 B |
| **RopeAppend**               | 100       |        **99,975.470 ns** |        354.2195 ns |        295.7894 ns |       100,077.271 ns |    1.7090 |         - |         - |     28616 B |
| ListInsert               | 100       |     9,642,900.684 ns |    184,668.9310 ns |    181,369.5827 ns |     9,663,835.156 ns |  734.3750 |  734.3750 |  734.3750 |  16748871 B |
| **RopeInsert**               | 100       |        **56,575.275 ns** |        334.5869 ns |        312.9728 ns |        56,558.954 ns |    1.6479 |         - |         - |     28000 B |
| ListSplitConcat          | 100       |       130,613.880 ns |      1,357.2378 ns |      1,269.5611 ns |       130,283.618 ns |   41.5039 |   41.5039 |   41.5039 |    197134 B |
| **RopeSplitConcat**          | 100       |         **4,976.573 ns** |         42.9692 ns |         40.1934 ns |         4,988.638 ns |    2.0065 |         - |         - |     33600 B |
| ListAppend               | 1000      |    35,500,738.500 ns |    720,812.8811 ns |  2,125,333.0432 ns |    35,504,653.571 ns | 3928.5714 | 3928.5714 | 3928.5714 | 134448581 B |
| **RopeAppend**               | 1000      |    **33,409,155.556 ns** |     85,742.3985 ns |     80,203.4927 ns |    33,411,353.333 ns |   66.6667 |         - |         - |   1523339 B |
| ListInsert               | 1000      | 1,384,004,000.000 ns | 13,382,115.3563 ns | 11,174,672.4015 ns | 1,385,487,300.000 ns | 3000.0000 | 3000.0000 | 3000.0000 | 134448640 B |
| **RopeInsert**               | 1000      |     **6,832,976.615 ns** |     80,962.1713 ns |     75,732.0653 ns |     6,874,546.875 ns |   15.6250 |         - |         - |    280003 B |
| ListSplitConcat          | 1000      |     1,028,210.003 ns |      3,725.0938 ns |      3,302.1986 ns |     1,027,935.254 ns |   41.0156 |   41.0156 |   41.0156 |    197135 B |
| **RopeSplitConcat**          | 1000      |        **50,068.350 ns** |        279.3499 ns |        261.3041 ns |        50,064.090 ns |   20.0806 |         - |         - |    336000 B |