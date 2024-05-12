# Rope

![logo](https://raw.githubusercontent.com/FlatlinerDOA/Rope/main/logo.png)

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


## License and Acknowledgements
Licensed MIT.
Portions of this code are Apache 2.0 License where nominated.

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

|Feature|Rope&lt;T&gt;|StringBuilder|List{T}|ReadOnlyMemory{T}|ImmutableList{T}|ImmutableArray{T}|
|-------|-------------|-------------|-------|-----------------|----------------|-----------------|
|Supports items of any type|✅|❌|✅|✅|✅|✅|
|Immutable edits|✅|❌|❌|❌|✅|✅|✅|
|Thread safe|✅<sup>1.</sup>|❌|❌|✅<sup>1.</sup>|✅|✅|✅|
|Copy free Append (avoid double allocations)|✅|✅|❌|❌|❌|❌|
|Copy free Insert|✅|✅|❌|❌|❌|❌|
|Copy free Remove|✅|❌|❌|❌|❌|❌|
|Copy free split|✅|❌|❌|❌|❌|❌|
|GC Friendly (No LOH, stays in Gen 0)|✅|❌|❌|✅|❌|✅|
|Create()|O(1)|O(N)|O(N)|O(1)|O(N)|O(N)|
|this[]|O(log N)|O(log N)|O(1)|O(1)|O(log N)|O(1)|
|Add|O(1) <sup>2.</sup>|O(log N)|O(1) <sup>3.</sup>|O(N) <sup>4.</sup>|O(log N)|O(N) <sup>4.</sup>|
|Value-like Equality <sup>5.</sup>|✅|❌|❌|❌|❌|❌|
|More than 2 billion elements (long index)|✅|❌|❌|❌|❌|❌|

* <sup>1.</sup> Thread safe as long as initial Array is not modified.
* <sup>2.</sup> Average is case O(1) (amortized). Worst case is O(log N) when a rebalance is required.
* <sup>3.</sup> Average is case O(1) (amortized). Worst case is O(N) when capacity increase is required.
* <sup>4.</sup> Copying to a new instance is required.
* <sup>5.</sup> Structural and reference invariant GetHashCode and Equals comparison.

# Performance

## Performance - AddRange

![AddRange](https://raw.githubusercontent.com/FlatlinerDOA/Rope/main/benchmarks/results/Benchmarks.AppendRange-barplot.png)

Working with a string of length - 32644 characters. - MaxLeafLength = ~32kb, Max Depth = 46

| Method        | EditCount | Mean             | Error          | StdDev         | Median           | Gen0      | Gen1      | Gen2      | Allocated  |
|-------------- |---------- |-----------------:|---------------:|---------------:|-----------------:|----------:|----------:|----------:|-----------:|
| **Rope**          | **10**        |         **81.34 ns** |       **1.625 ns** |       **3.356 ns** |         **79.70 ns** |    **0.0473** |         **-** |         **-** |      **792 B** |
| StringBuilder | 10        |     19,888.60 ns |     199.916 ns |     187.001 ns |     19,841.24 ns |   43.1213 |   35.2478 |         - |   723272 B |
| List          | 10        |  1,238,461.90 ns |   8,023.175 ns |   7,504.883 ns |  1,237,642.38 ns |  498.0469 |  498.0469 |  498.0469 |  2098128 B |
| **Rope**          | **100**       |      **6,326.66 ns** |      **46.375 ns** |      **41.110 ns** |      **6,318.72 ns** |    **2.1973** |    **0.0534** |         **-** |    **36792 B** |
| StringBuilder | 100       |    298,139.65 ns |   5,226.588 ns |   4,080.576 ns |    298,538.06 ns |  395.9961 |  383.7891 |         - |  6640955 B |
| List          | 100       |  9,485,481.04 ns |  75,729.841 ns |  70,837.740 ns |  9,492,607.81 ns |  734.3750 |  734.3750 |  734.3750 | 16781223 B |
| **Rope**          | **500**       |    **126,232.79 ns** |     **783.746 ns** |     **694.770 ns** |    **126,060.33 ns** |   **39.3066** |    **6.8359** |         **-** |   **660096 B** |
| StringBuilder | 500       |  6,498,547.88 ns | 109,273.449 ns |  96,868.066 ns |  6,494,527.34 ns | 2679.6875 | 2664.0625 |  945.3125 | 32947786 B |
| List          | 500       | 43,939,108.48 ns | 527,171.657 ns | 493,116.695 ns | 44,072,354.55 ns | 2909.0909 | 2909.0909 | 2909.0909 | 67126462 B |


##  Performance - InsertRange

![InsertRange](https://raw.githubusercontent.com/FlatlinerDOA/Rope/main/benchmarks/results/Benchmarks.InsertRange-barplot.png)

| Method        | EditCount | Mean               | Error           | StdDev          | Gen0      | Gen1      | Gen2      | Allocated    |
|-------------- |---------- |-------------------:|----------------:|----------------:|----------:|----------:|----------:|-------------:|
| **RopeOfChar**    | **10**        |           **408.8 ns** |        **147.7 ns** |         **8.10 ns** |    **0.2151** |    **0.0005** |         **-** |      **3.52 KB** |
| ListOfChar    | 10        |       691,128.2 ns |    306,483.6 ns |    16,799.40 ns |  499.0234 |  499.0234 |  499.0234 |   2052.84 KB |
| StringBuilder | 10        |        18,397.2 ns |        350.5 ns |        19.21 ns |   43.1213 |   31.3416 |         - |    706.32 KB |
| **RopeOfChar**    | **100**       |         **4,038.7 ns** |      **1,844.8 ns** |       **101.12 ns** |    **2.1515** |    **0.0305** |         **-** |     **35.16 KB** |
| ListOfChar    | 100       |     9,995,754.2 ns |  4,670,207.9 ns |   255,989.85 ns |  734.3750 |  734.3750 |  734.3750 |  16420.48 KB |
| StringBuilder | 100       |       286,050.8 ns |     48,643.1 ns |     2,666.29 ns |  395.9961 |  379.8828 |         - |    6485.3 KB |
| **RopeOfChar**    | **1000**      |        **39,097.6 ns** |      **5,179.6 ns** |       **283.91 ns** |   **21.4844** |    **2.7466** |         **-** |    **351.56 KB** |
| ListOfChar    | 1000      | 1,359,698,366.7 ns | 60,718,583.3 ns | 3,328,190.43 ns | 3000.0000 | 3000.0000 | 3000.0000 | 131361.66 KB |
| StringBuilder | 1000      |    21,847,408.3 ns | 54,123,876.7 ns | 2,966,712.32 ns | 5406.2500 | 5375.0000 | 1625.0000 |  64275.69 KB |


##  Performance - Split then Concat

![Split then Concat](https://raw.githubusercontent.com/FlatlinerDOA/Rope/main/benchmarks/results/Benchmarks.SplitThenConcat-barplot.png)

| Method        | EditCount | Mean            | Error           | StdDev        | Gen0      | Gen1     | Gen2     | Allocated   |
|-------------- |---------- |----------------:|----------------:|--------------:|----------:|---------:|---------:|------------:|
| **ListOfChar**    | **10**        |    **541,450.0 ns** |    **99,808.39 ns** |   **5,470.83 ns** |   **41.0156** |  **41.0156** |  **41.0156** |   **256.73 KB** |
| RopeOfChar    | 10        |        418.1 ns |        72.73 ns |       3.99 ns |    0.2580 |        - |        - |     4.22 KB |
| StringBuilder | 10        |     15,773.0 ns |     8,684.35 ns |     476.02 ns |   23.5291 |  11.7493 |        - |   385.66 KB |
| **ListOfChar**    | **100**       |  **4,610,185.9 ns** | **2,158,247.14 ns** | **118,300.81 ns** |   **39.0625** |  **39.0625** |  **39.0625** |   **259.55 KB** |
| RopeOfChar    | 100       |      4,299.8 ns |     1,127.39 ns |      61.80 ns |    2.5787 |        - |        - |    42.19 KB |
| StringBuilder | 100       |    164,727.5 ns |   306,656.89 ns |  16,808.90 ns |  199.9512 |  70.5566 |        - |  3278.66 KB |
| **ListOfChar**    | **1000**      | **45,744,348.5 ns** | **9,522,133.89 ns** | **521,940.29 ns** |         **-** |        **-** |        **-** |   **288.79 KB** |
| RopeOfChar    | 1000      |     44,982.3 ns |     2,172.21 ns |     119.07 ns |   25.8179 |        - |        - |   421.88 KB |
| StringBuilder | 1000      |  2,007,108.4 ns | 2,792,873.37 ns | 153,086.81 ns | 1968.7500 | 498.0469 | 123.0469 | 32208.78 KB |

##  Performance - Create New

![Create New Empty](https://raw.githubusercontent.com/FlatlinerDOA/Rope/main/benchmarks/results/Benchmarks.CreateNewEmpty-barplot.png)
![Create New With Length 10](https://raw.githubusercontent.com/FlatlinerDOA/Rope/main/benchmarks/results/Benchmarks.CreateNewWithLength10-barplot.png)


## Performance - Equals

![Equals](https://raw.githubusercontent.com/FlatlinerDOA/Rope/main/benchmarks/results/Benchmarks.Equals-barplot.png)

| Method        | Length | Mean       | Error     | StdDev    | Allocated |
|-------------- |------- |-----------:|----------:|----------:|----------:|
| **Rope&lt;char&gt;**    | **10**     |   **4.683 ns** | **0.0831 ns** | **0.0694 ns** |         **-** |
| StringBuilder | 10     |   5.094 ns | 0.0441 ns | 0.0391 ns |         - |
| string        | 10     |   2.155 ns | 0.0129 ns | 0.0107 ns |         - |
| **Rope&lt;char&gt;**    | **100**    |   **4.659 ns** | **0.0323 ns** | **0.0270 ns** |         **-** |
| StringBuilder | 100    |   6.402 ns | 0.0521 ns | 0.0487 ns |         - |
| string        | 100    |   4.050 ns | 0.0206 ns | 0.0193 ns |         - |
| **Rope&lt;char&gt;**    | **1000**   |   **4.653 ns** | **0.0364 ns** | **0.0323 ns** |         **-** |
| StringBuilder | 1000   |  27.302 ns | 0.0866 ns | 0.0810 ns |         - |
| string        | 1000   |  28.123 ns | 0.0802 ns | 0.0750 ns |         - |
| **Rope&lt;char&gt;**    | **10000**  |   **4.600 ns** | **0.0124 ns** | **0.0116 ns** |         **-** |
| StringBuilder | 10000  | 265.757 ns | 0.5245 ns | 0.4907 ns |         - |
| string        | 10000  | 275.796 ns | 0.4692 ns | 0.4160 ns |         - |


## Performance - IndexOf

![IndexOf](https://raw.githubusercontent.com/FlatlinerDOA/Rope/main/benchmarks/results/Benchmarks.IndexOf-barplot.png)

| Method                      | Length | Mean         | Error        | StdDev     | Gen0   | Allocated |
|---------------------------- |------- |-------------:|-------------:|-----------:|-------:|----------:|
| **Rope**                        | **10**     |     **31.73 ns** |    **23.876 ns** |   **1.309 ns** | **0.0172** |     **288 B** |
| **Rope**                        | **100**    |     **34.70 ns** |    **21.025 ns** |   **1.152 ns** | **0.0172** |     **288 B** |
| **Rope**                        | **1000**   |     **78.29 ns** |    **29.446 ns** |   **1.614 ns** | **0.0172** |     **288 B** |
| **Rope**                        | **10000**  |     **96.90 ns** |    **40.885 ns** |   **2.241 ns** | **0.0172** |     **288 B** |
| **IndexOf**                     | **10**     |     **96.39 ns** |     **9.489 ns** |   **0.520 ns** | **0.0430** |     **720 B** |
| &#39;IndexOf (Fragmented Find)&#39; | 10     |     92.93 ns |    17.273 ns |   0.947 ns | 0.0430 |     720 B |
| **IndexOf**                     | **100**    |    **104.47 ns** |    **27.081 ns** |   **1.484 ns** | **0.0430** |     **720 B** |
| &#39;IndexOf (Fragmented Find)&#39; | 100    |     93.17 ns |    13.728 ns |   0.752 ns | 0.0430 |     720 B |
| **IndexOf**                     | **1000**   |    **493.07 ns** |    **89.003 ns** |   **4.879 ns** | **0.0420** |     **704 B** |
| &#39;IndexOf (Fragmented Find)&#39; | 1000   |     88.98 ns |    36.926 ns |   2.024 ns | 0.0343 |     576 B |
| **IndexOf**                     | **10000**  |    **876.67 ns** |   **111.080 ns** |   **6.089 ns** | **0.0763** |    **1280 B** |
| &#39;IndexOf (Fragmented Find)&#39; | 10000  |  1,679.10 ns |    51.484 ns |   2.822 ns | 0.0916 |    1552 B |
| **String**                      | **10**     |     **17.97 ns** |     **5.800 ns** |   **0.318 ns** | **0.0029** |      **48 B** |
| **String**                      | **100**    |  **3,925.06 ns** |   **951.757 ns** |  **52.169 ns** | **0.0076** |     **224 B** |
| **String**                      | **1000**   | **23,939.22 ns** |   **962.312 ns** |  **52.748 ns** | **0.0916** |    **2024 B** |
| **String**                      | **10000**  | **36,962.96 ns** | **6,578.140 ns** | **360.570 ns** | **1.1597** |   **20024 B** |
