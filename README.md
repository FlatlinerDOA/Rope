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

// 
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
|UTF-8 strings (Rope&lt;byte&gt;)| ✅ |❌|
|UTF-32 strings (Rope&lt;Rune&gt;)| ✅ |❌|
|Structural invariant GetHashCode| ✅ |❌|
|Structural invariant Equals| ✅ |❌|


### Performance and Memory Allocation Comparison

Working with a string of length - 32644 characters. - MaxLeafLength = ~32kb, Max Depth = 46

| Method                            | Edits | Mean                 | Error             | StdDev            | Gen0      | Gen1      | Gen2      | Allocated  |
|---------------------------------- |--------------- |---------------------:|------------------:|------------------:|----------:|----------:|----------:|-----------:|
| **StringBuilderConstructionOverhead** | **10**             |             **7.809 ns** |         **0.0550 ns** |         **0.0515 ns** |    **0.0062** |         **-** |         **-** |      **104 B** |
| RopeConstructionOverhead          | 10             |             3.859 ns |         0.0901 ns |         0.1073 ns |    0.0033 |         - |         - |       56 B |
| StringBuilderAppend               | 10             |        40,534.935 ns |       810.3675 ns |     2,148.9796 ns |   42.9688 |   31.1890 |         - |   721160 B |
| RopeAppend                        | 10             |           317.650 ns |         1.3256 ns |         1.2399 ns |    0.0367 |         - |         - |      616 B |
| StringBuilderInsert               | 10             |        43,459.897 ns |     1,221.2060 ns |     3,600.7536 ns |   42.9688 |   27.3438 |         - |   721160 B |
| RopeInsert                        | 10             |           781.521 ns |         9.3612 ns |         8.7565 ns |    0.1669 |         - |         - |     2800 B |
| StringBuilderSplitConcat          | 10             |        28,432.049 ns |       416.0166 ns |       324.7984 ns |   23.4680 |   11.7188 |         - |   393720 B |
| RopeSplitConcat                   | 10             |           493.856 ns |         4.8706 ns |         4.3176 ns |    0.2003 |         - |         - |     3360 B |
| **StringBuilderConstructionOverhead** | **100**            |             **7.938 ns** |         **0.1647 ns** |         **0.1540 ns** |    **0.0062** |         **-** |         **-** |      **104 B** |
| RopeConstructionOverhead          | 100            |             3.956 ns |         0.0993 ns |         0.1220 ns |    0.0033 |         - |         - |       56 B |
| StringBuilderAppend               | 100            |       454,065.647 ns |    16,428.4540 ns |    48,439.6672 ns |  394.5313 |  382.3242 |         - |  6621560 B |
| RopeAppend                        | 100            |       957,533.053 ns |     9,832.7415 ns |     9,197.5525 ns |    7.8125 |         - |         - |   144816 B |
| StringBuilderInsert               | 100            |       515,805.869 ns |    12,467.5560 ns |    36,368.4461 ns |  394.5313 |  378.9063 |         - |  6621560 B |
| RopeInsert                        | 100            |        50,168.761 ns |       136.7082 ns |       127.8769 ns |    1.6479 |         - |         - |    28000 B |
| StringBuilderSplitConcat          | 100            |       248,479.532 ns |     5,598.8172 ns |    16,508.2389 ns |  199.7070 |   99.6094 |         - |  3347160 B |
| RopeSplitConcat                   | 100            |         5,015.996 ns |        98.0012 ns |       143.6490 ns |    2.0065 |         - |         - |    33600 B |
| **StringBuilderConstructionOverhead** | **1000**           |             **8.208 ns** |         **0.1241 ns** |         **0.1100 ns** |    **0.0062** |         **-** |         **-** |      **104 B** |
| RopeConstructionOverhead          | 1000           |             3.755 ns |         0.0921 ns |         0.1983 ns |    0.0033 |         - |         - |       56 B |
| StringBuilderAppend               | 1000           |    23,822,775.472 ns |   474,466.0536 ns |   879,453.2527 ns | 5437.5000 | 5406.2500 | 1625.0000 | 65637900 B |
| RopeAppend                        | 1000           | 1,475,919,300.000 ns | 2,928,700.4503 ns | 2,596,216.6654 ns | 1000.0000 |         - |         - | 26428816 B |
| StringBuilderInsert               | 1000           |    24,622,036.938 ns |   551,434.5412 ns | 1,625,917.1864 ns | 5437.5000 | 5406.2500 | 1625.0000 | 65627902 B |
| RopeInsert                        | 1000           |     6,350,752.604 ns |    92,691.0537 ns |    86,703.2692 ns |   15.6250 |         - |         - |   280003 B |
| StringBuilderSplitConcat          | 1000           |     1,990,366.980 ns |    56,738.8359 ns |   167,295.7379 ns | 1962.8906 |  494.1406 |  125.0000 | 32881603 B |
| RopeSplitConcat                   | 1000           |        54,579.363 ns |       872.4714 ns |       773.4232 ns |   20.0806 |         - |         - |   336000 B |
