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


### Performance and Memory Allocation Comparison with StringBuilder

Working with a string of length - 32644 characters. - MaxLeafLength = ~32kb, Max Depth = 46

| Method                            | EditCount | Mean               | Error              | StdDev            | Gen0      | Gen1      | Gen2      | Allocated  |
|---------------------------------- |---------- |-------------------:|-------------------:|------------------:|----------:|----------:|----------:|-----------:|
| StringBuilderConstructionOverhead | 10        |           6.660 ns |          3.8446 ns |         0.2107 ns |    0.0062 |         - |         - |      104 B |
| RopeConstructionOverhead          | 10        |           3.467 ns |         10.3511 ns |         0.5674 ns |    0.0033 |         - |         - |       56 B |
| StringBuilderAppend               | 10        |      18,937.502 ns |     36,176.2486 ns |     1,982.9423 ns |   42.9688 |   31.2195 |         - |   721160 B |
| RopeAppend                        | 10        |         326.512 ns |        105.4630 ns |         5.7808 ns |    0.0367 |         - |         - |      616 B |
| StringBuilderInsert               | 10        |      22,208.297 ns |     20,717.6339 ns |     1,135.6034 ns |   42.9688 |   27.3438 |         - |   721160 B |
| RopeInsert                        | 10        |       1,048.996 ns |         67.6687 ns |         3.7092 ns |    0.1659 |         - |         - |     2800 B |
| StringBuilderSplitConcat          | 10        |      16,936.850 ns |      7,790.6697 ns |       427.0329 ns |   23.4680 |   11.7188 |         - |   393720 B |
| RopeSplitConcat                   | 10        |         498.009 ns |        176.2005 ns |         9.6581 ns |    0.2003 |         - |         - |     3360 B |
| StringBuilderConstructionOverhead | 100       |           6.393 ns |          0.8614 ns |         0.0472 ns |    0.0062 |         - |         - |      104 B |
| RopeConstructionOverhead          | 100       |           3.065 ns |          1.4724 ns |         0.0807 ns |    0.0033 |         - |         - |       56 B |
| StringBuilderAppend               | 100       |     265,767.204 ns |    110,624.5007 ns |     6,063.7022 ns |  394.5313 |  382.3242 |         - |  6621560 B |
| RopeAppend                        | 100       |     775,989.095 ns |    239,236.3722 ns |    13,113.3528 ns |    7.8125 |         - |         - |   144816 B |
| StringBuilderInsert               | 100       |     288,972.005 ns |     77,008.2196 ns |     4,221.0804 ns |  394.5313 |  378.9063 |         - |  6621560 B |
| RopeInsert                        | 100       |      54,980.349 ns |      6,474.1330 ns |       354.8691 ns |    1.6479 |         - |         - |    28000 B |
| StringBuilderSplitConcat          | 100       |     158,307.430 ns |    185,564.9036 ns |    10,171.4385 ns |  199.9512 |   99.8535 |         - |  3347160 B |
| RopeSplitConcat                   | 100       |       5,244.930 ns |        777.7459 ns |        42.6309 ns |    2.0065 |         - |         - |    33600 B |
| StringBuilderConstructionOverhead | 1000      |           6.375 ns |          2.9867 ns |         0.1637 ns |    0.0062 |         - |         - |      104 B |
| RopeConstructionOverhead          | 1000      |           3.062 ns |          1.1840 ns |         0.0649 ns |    0.0033 |         - |         - |       56 B |
| StringBuilderAppend               | 1000      |  23,347,470.833 ns | 25,992,020.2280 ns | 1,424,710.3348 ns | 5437.5000 | 5406.2500 | 1625.0000 | 65637631 B |
| RopeAppend                        | 1000      | 955,351,466.667 ns | 55,143,071.0173 ns | 3,022,577.7944 ns | 1000.0000 |         - |         - | 26428816 B |
| StringBuilderInsert               | 1000      |  22,830,273.958 ns | 35,391,707.9669 ns | 1,939,938.9376 ns | 5437.5000 | 5406.2500 | 1625.0000 | 65627647 B |
| RopeInsert                        | 1000      |   6,440,357.292 ns |    769,839.8628 ns |    42,197.5206 ns |   15.6250 |         - |         - |   280003 B |
| StringBuilderSplitConcat          | 1000      |   1,576,391.211 ns |    499,400.6799 ns |    27,373.8364 ns | 1962.8906 |  494.1406 |  125.0000 | 32881603 B |
| RopeSplitConcat                   | 1000      |      49,510.901 ns |      2,645.9102 ns |       145.0313 ns |   20.0806 |         - |         - |   336000 B |

### Performance and Memory Allocation Comparison with List&lt;T&gt;

| Method                   | EditCount | Mean                 | Error                 | StdDev              | Gen0      | Gen1      | Gen2      | Allocated   |
|------------------------- |---------- |---------------------:|----------------------:|--------------------:|----------:|----------:|----------:|------------:|
| ListConstructionOverhead | 10        |             2.056 ns |             0.5388 ns |           0.0295 ns |    0.0019 |         - |         - |        32 B |
| RopeConstructionOverhead | 10        |             3.409 ns |            12.4648 ns |           0.6832 ns |    0.0033 |         - |         - |        56 B |
| ListAppend               | 10        |       616,994.108 ns |     1,001,260.0613 ns |      54,882.4426 ns |  499.0234 |  499.0234 |  499.0234 |   2036400 B |
| RopeAppend               | 10        |           315.757 ns |            13.0887 ns |           0.7174 ns |    0.0367 |         - |         - |       616 B |
| ListInsert               | 10        |       706,230.827 ns |       533,914.1540 ns |      29,265.6364 ns |  499.0234 |  499.0234 |  499.0234 |   2036400 B |
| RopeInsert               | 10        |         1,040.191 ns |            52.7603 ns |           2.8920 ns |    0.1659 |         - |         - |      2800 B |
| ListSplitConcat          | 10        |        40,739.286 ns |        17,913.1578 ns |         981.8806 ns |   41.6260 |   41.6260 |   41.6260 |    197134 B |
| RopeSplitConcat          | 10        |           489.633 ns |            87.2746 ns |           4.7838 ns |    0.2003 |         - |         - |      3360 B |
| ListConstructionOverhead | 100       |             2.536 ns |             9.2355 ns |           0.5062 ns |    0.0019 |         - |         - |        32 B |
| RopeConstructionOverhead | 100       |             3.230 ns |             3.3654 ns |           0.1845 ns |    0.0033 |         - |         - |        56 B |
| ListAppend               | 100       |     4,514,265.625 ns |     1,902,140.1530 ns |     104,262.7203 ns |  742.1875 |  742.1875 |  742.1875 |  16748870 B |
| RopeAppend               | 100       |       763,204.329 ns |       274,428.0687 ns |      15,042.3285 ns |    7.8125 |         - |         - |    144816 B |
| ListInsert               | 100       |    10,058,506.771 ns |     3,884,354.5719 ns |     212,914.5813 ns |  734.3750 |  734.3750 |  734.3750 |  16748871 B |
| RopeInsert               | 100       |        55,784.001 ns |         6,018.0238 ns |         329.8682 ns |    1.6479 |         - |         - |     28000 B |
| ListSplitConcat          | 100       |       130,900.138 ns |        19,537.7403 ns |       1,070.9295 ns |   41.5039 |   41.5039 |   41.5039 |    197134 B |
| RopeSplitConcat          | 100       |         4,849.854 ns |           254.8449 ns |          13.9689 ns |    2.0065 |         - |         - |     33600 B |
| ListConstructionOverhead | 1000      |             2.181 ns |             0.6160 ns |           0.0338 ns |    0.0019 |         - |         - |        32 B |
| RopeConstructionOverhead | 1000      |             3.662 ns |             4.7910 ns |           0.2626 ns |    0.0033 |         - |         - |        56 B |
| ListAppend               | 1000      |    38,171,446.154 ns |    64,503,216.0755 ns |   3,535,638.9295 ns | 3923.0769 | 3923.0769 | 3923.0769 | 134448581 B |
| RopeAppend               | 1000      |   992,371,966.667 ns |   172,595,767.5594 ns |   9,460,556.4183 ns | 1000.0000 |         - |         - |  26428816 B |
| ListInsert               | 1000      | 1,694,145,733.333 ns | 4,669,673,708.1417 ns | 255,960,573.0516 ns | 3000.0000 | 3000.0000 | 3000.0000 | 134448640 B |
| RopeInsert               | 1000      |     6,539,033.073 ns |       663,740.5566 ns |      36,381.8596 ns |   15.6250 |         - |         - |    280003 B |
| ListSplitConcat          | 1000      |     1,035,388.086 ns |        62,229.0122 ns |       3,410.9821 ns |   41.0156 |   41.0156 |   41.0156 |    197135 B |
| RopeSplitConcat          | 1000      |        52,198.216 ns |        28,657.5458 ns |       1,570.8168 ns |   20.0806 |         - |         - |    336000 B |