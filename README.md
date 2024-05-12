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

![AddRange](/benchmarks/results/Benchmarks.AddRange-barplot.png)

Working with a string of length - 32644 characters. - MaxLeafLength = ~32kb, Max Depth = 46

| Method        | EditCount | Mean            | Error         | StdDev          | Gen0      | Gen1      | Gen2      | Allocated  |
|-------------- |---------- |----------------:|--------------:|----------------:|----------:|----------:|----------:|-----------:|
| **Rope**          | **10**        |        **322.6 ns** |       **1.56 ns** |         **1.30 ns** |    **0.0496** |         **-** |         **-** |      **832 B** |
| StringBuilder | 10        |     20,424.1 ns |      46.48 ns |        41.20 ns |   43.1213 |   35.2478 |         - |   723272 B |
| List          | 10        |  1,170,757.8 ns |  23,156.59 ns |    21,660.69 ns |  498.0469 |  498.0469 |  498.0469 |  2098128 B |
| **Rope**          | **100**       |     **22,060.6 ns** |     **137.07 ns** |       **128.22 ns** |    **2.3499** |    **0.0610** |         **-** |    **39584 B** |
| StringBuilder | 100       |    293,721.1 ns |   2,960.33 ns |     2,769.09 ns |  395.9961 |  383.7891 |         - |  6640952 B |
| List          | 100       |  9,110,871.4 ns | 179,104.14 ns |   219,955.97 ns |  734.3750 |  734.3750 |  734.3750 | 16781223 B |
| **Rope**          | **500**       |    **415,645.5 ns** |   **1,144.69 ns** |       **893.70 ns** |   **41.9922** |    **6.8359** |         **-** |   **702864 B** |
| StringBuilder | 500       |  2,561,640.0 ns |  24,520.41 ns |    22,936.41 ns | 2687.5000 | 2671.8750 |  949.2188 | 32947836 B |
| List          | 500       | 42,869,839.1 ns | 831,131.28 ns | 1,051,114.95 ns | 2916.6667 | 2916.6667 | 2916.6667 | 67126461 B |


## Performance - AddRange (Immutable Collections)

![AddRangeImmutable](/benchmarks/results/Benchmarks.AddRangeImmutable-barplot.png)


| Method                 | EditCount | Mean             | Error           | StdDev          | Gen0       | Gen1       | Gen2       | Allocated   |
|----------------------- |---------- |-----------------:|----------------:|----------------:|-----------:|-----------:|-----------:|------------:|
| **Rope**                   | **10**        |         **356.9 ns** |         **6.63 ns** |         **6.20 ns** |     **0.0496** |          **-** |          **-** |       **832 B** |
| ImmutableList
.Builder | 10        |   7,700,966.8 ns |   152,284.06 ns |   232,553.85 ns |   992.1875 |   976.5625 |          - |  16633835 B |
| ImmutableList          | 10        |   7,328,406.3 ns |   144,002.40 ns |   134,699.94 ns |   992.1875 |   976.5625 |          - |  16635411 B |
| ImmutableArray         | 10        |     457,004.7 ns |     8,949.78 ns |    14,452.23 ns |   996.0938 |   996.0938 |   996.0938 |   4335446 B |
| **Rope**                   | **100**       |      **22,539.8 ns** |       **126.04 ns** |       **117.90 ns** |     **2.3499** |     **0.0610** |          **-** |     **39584 B** |
| ImmutableList
.Builder | 100       | 214,688,974.5 ns | 4,289,220.24 ns | 8,365,794.66 ns | 11000.0000 | 10500.0000 |  2000.0000 | 152739984 B |
| ImmutableList          | 100       | 206,931,377.8 ns | 4,000,014.25 ns | 3,741,615.81 ns | 11000.0000 | 10666.6667 |  2000.0000 | 152768739 B |
| ImmutableArray         | 100       | 102,947,759.8 ns | 2,231,458.55 ns | 6,544,481.64 ns | 24400.0000 | 24400.0000 | 24400.0000 | 338327974 B |


##  Performance - InsertRange

![InsertRange](/benchmarks/results/Benchmarks.InsertRange-barplot.png)

| Method        | EditCount | Mean             | Error         | StdDev        | Median           | Gen0      | Gen1      | Gen2      | Allocated    |
|-------------- |---------- |-----------------:|--------------:|--------------:|-----------------:|----------:|----------:|----------:|-------------:|
| **RopeOfChar**    | **10**        |         **1.400 μs** |     **0.0120 μs** |     **0.0094 μs** |         **1.402 μs** |    **0.1774** |         **-** |         **-** |      **2.92 KB** |
| ListOfChar    | 10        |       741.599 μs |     2.4556 μs |     2.0506 μs |       741.928 μs |  499.0234 |  499.0234 |  499.0234 |   2052.84 KB |
| StringBuilder | 10        |        18.175 μs |     0.1077 μs |     0.0955 μs |        18.165 μs |   43.1213 |   31.3416 |         - |    706.32 KB |
| **RopeOfChar**    | **100**       |        **35.324 μs** |     **0.4283 μs** |     **0.3796 μs** |        **35.221 μs** |    **3.8452** |    **0.0610** |         **-** |      **63.3 KB** |
| ListOfChar    | 100       |    10,949.444 μs |   217.4859 μs |   444.2661 μs |    10,756.000 μs |  734.3750 |  734.3750 |  734.3750 |  16420.48 KB |
| StringBuilder | 100       |       315.408 μs |     4.5317 μs |     4.2390 μs |       314.411 μs |  395.9961 |  379.8828 |         - |    6485.3 KB |
| **RopeOfChar**    | **1000**      |     **1,900.043 μs** |     **6.0456 μs** |     **5.0484 μs** |     **1,898.135 μs** |  **183.5938** |   **72.2656** |         **-** |   **3022.17 KB** |
| ListOfChar    | 1000      | 1,433,052.443 μs | 5,397.8058 μs | 4,785.0142 μs | 1,432,979.650 μs | 3000.0000 | 3000.0000 | 3000.0000 | 131361.66 KB |
| StringBuilder | 1000      |    24,700.136 μs |   905.6912 μs | 2,670.4509 μs |    25,727.847 μs | 5406.2500 | 5375.0000 | 1625.0000 |  64277.44 KB |


##  Performance - Split then Concat

![Split then Concat](/benchmarks/results/Benchmarks.SplitThenConcat-barplot.png)

| Method        | EditCount | Mean            | Error         | StdDev        | Gen0      | Gen1     | Gen2     | Allocated  |
|-------------- |---------- |----------------:|--------------:|--------------:|----------:|---------:|---------:|-----------:|
| **RopeOfChar**    | **10**        |        **573.1 ns** |       **4.15 ns** |       **3.88 ns** |    **0.0515** |        **-** |        **-** |      **864 B** |
| StringBuilder | 10        |     17,382.5 ns |     347.14 ns |     439.02 ns |   23.5291 |  11.7493 |        - |   394912 B |
| ListOfChar    | 10        |    521,531.2 ns |   3,077.00 ns |   2,727.68 ns |   41.0156 |  41.0156 |  41.0156 |   262894 B |
| **RopeOfChar**    | **100**       |      **5,517.7 ns** |      **18.94 ns** |      **14.79 ns** |    **0.4807** |        **-** |        **-** |     **8064 B** |
| StringBuilder | 100       |    153,816.0 ns |   2,649.38 ns |   2,212.35 ns |  199.9512 |  70.5566 |        - |  3357352 B |
| ListOfChar    | 100       |  4,481,838.8 ns |  38,092.78 ns |  33,768.26 ns |   39.0625 |  39.0625 |  39.0625 |   265776 B |
| **RopeOfChar**    | **1000**      |     **56,027.3 ns** |     **322.81 ns** |     **286.16 ns** |    **4.7607** |        **-** |        **-** |    **80064 B** |
| StringBuilder | 1000      |  1,519,246.3 ns |  30,125.60 ns |  41,236.26 ns | 1968.7500 | 498.0469 | 123.0469 | 32981794 B |
| ListOfChar    | 1000      | 44,472,061.7 ns | 370,549.50 ns | 346,612.23 ns |         - |        - |        - |   294593 B |


## Performance - Equals

![Equals](/benchmarks/results/Benchmarks.Equals-barplot.png)

| Method        | Length | Mean       | Error     | StdDev    | Allocated |
|-------------- |------- |-----------:|----------:|----------:|----------:|
| **Rope&lt;char&gt;**    | **10**     |   **4.640 ns** | **0.0256 ns** | **0.0239 ns** |         **-** |
| StringBuilder | 10     |   5.057 ns | 0.0254 ns | 0.0237 ns |         - |
| string        | 10     |   2.124 ns | 0.0047 ns | 0.0039 ns |         - |
| **Rope&lt;char&gt;**    | **100**    |   **4.628 ns** | **0.0272 ns** | **0.0255 ns** |         **-** |
| StringBuilder | 100    |   6.404 ns | 0.0120 ns | 0.0112 ns |         - |
| string        | 100    |   4.102 ns | 0.0201 ns | 0.0188 ns |         - |
| **Rope&lt;char&gt;**    | **1000**   |   **4.611 ns** | **0.0253 ns** | **0.0236 ns** |         **-** |
| StringBuilder | 1000   |  27.342 ns | 0.1317 ns | 0.1232 ns |         - |
| string        | 1000   |  28.369 ns | 0.1189 ns | 0.1112 ns |         - |
| **Rope&lt;char&gt;**    | **10000**  |   **4.646 ns** | **0.0172 ns** | **0.0161 ns** |         **-** |
| StringBuilder | 10000  | 274.231 ns | 1.8964 ns | 1.7739 ns |         - |
| string        | 10000  | 276.374 ns | 1.0101 ns | 0.8434 ns |         - |


## Performance - IndexOf

![IndexOf](/benchmarks/results/Benchmarks.IndexOf-barplot.png)

| Method                      | Length | Mean         | Error      | StdDev     | Median       | Gen0   | Allocated |
|---------------------------- |------- |-------------:|-----------:|-----------:|-------------:|-------:|----------:|
| **Rope**                        | **10**     |     **18.62 ns** |   **0.059 ns** |   **0.055 ns** |     **18.62 ns** | **0.0019** |      **32 B** |
| **Rope**                        | **100**    |     **24.91 ns** |   **0.080 ns** |   **0.075 ns** |     **24.89 ns** | **0.0019** |      **32 B** |
| **Rope**                        | **1000**   |     **55.89 ns** |   **0.101 ns** |   **0.089 ns** |     **55.89 ns** | **0.0019** |      **32 B** |
| **Rope**                        | **10000**  |     **83.87 ns** |   **0.109 ns** |   **0.091 ns** |     **83.88 ns** | **0.0019** |      **32 B** |
| **IndexOf**                     | **10**     |     **61.17 ns** |   **0.285 ns** |   **0.253 ns** |     **61.12 ns** | **0.0019** |      **32 B** |
| &#39;IndexOf (Fragmented Find)&#39; | 10     |     62.37 ns |   1.252 ns |   1.391 ns |     63.50 ns | 0.0019 |      32 B |
| **IndexOf**                     | **100**    |     **63.37 ns** |   **0.256 ns** |   **0.227 ns** |     **63.28 ns** | **0.0019** |      **32 B** |
| &#39;IndexOf (Fragmented Find)&#39; | 100    |     59.36 ns |   0.144 ns |   0.135 ns |     59.33 ns | 0.0019 |      32 B |
| **IndexOf**                     | **1000**   |    **588.18 ns** |   **1.596 ns** |   **1.493 ns** |    **588.58 ns** | **0.0191** |     **320 B** |
| &#39;IndexOf (Fragmented Find)&#39; | 1000   |    103.18 ns |   0.439 ns |   0.411 ns |    103.14 ns | 0.0114 |     192 B |
| **IndexOf**                     | **10000**  |  **1,158.36 ns** |   **2.464 ns** |   **2.305 ns** |  **1,157.89 ns** | **0.0629** |    **1056 B** |
| &#39;IndexOf (Fragmented Find)&#39; | 10000  |  2,245.40 ns |  28.287 ns |  26.460 ns |  2,234.64 ns | 0.0763 |    1328 B |
| **String**                      | **10**     |     **17.85 ns** |   **0.285 ns** |   **0.253 ns** |     **17.77 ns** | **0.0029** |      **48 B** |
| **String**                      | **100**    |  **3,933.89 ns** |  **62.442 ns** |  **55.353 ns** |  **3,922.67 ns** | **0.0076** |     **224 B** |
| **String**                      | **1000**   | **23,674.90 ns** | **205.594 ns** | **192.313 ns** | **23,758.94 ns** | **0.0916** |    **2024 B** |
| **String**                      | **10000**  | **36,379.40 ns** | **435.649 ns** | **407.506 ns** | **36,422.10 ns** | **1.1597** |   **20024 B** |


##  Performance - Create New

![Create New Empty](/benchmarks/results/Benchmarks.CreateNewEmpty-barplot.png)

| Method                | Mean      | Error     | StdDev    | Median    | Gen0   | Allocated |
|---------------------- |----------:|----------:|----------:|----------:|-------:|----------:|
| Rope&lt;char&gt;.Empty      | 0.0003 ns | 0.0006 ns | 0.0005 ns | 0.0000 ns |      - |         - |
| &#39;new List&lt;char&gt;()&#39;    | 2.2120 ns | 0.0305 ns | 0.0255 ns | 2.2146 ns | 0.0019 |      32 B |
| &#39;new StringBuilder()&#39; | 6.5696 ns | 0.1487 ns | 0.1712 ns | 6.6044 ns | 0.0062 |     104 B |


![Create New With Length 10](/benchmarks/results/Benchmarks.CreateNewWithLength10-barplot.png)

| Method                      | Mean      | Error     | StdDev    | Gen0   | Allocated |
|---------------------------- |----------:|----------:|----------:|-------:|----------:|
| string.ToRope()             |  5.815 ns | 0.1009 ns | 0.0943 ns | 0.0019 |      32 B |
| &#39;new Rope&lt;char&gt;(array)&#39;     |  5.430 ns | 0.0174 ns | 0.0154 ns | 0.0019 |      32 B |
| &#39;new List&lt;char&gt;(array)&#39;     | 16.955 ns | 0.1281 ns | 0.1135 ns | 0.0048 |      80 B |
| &#39;new StringBuilder(string)&#39; |  8.575 ns | 0.0361 ns | 0.0338 ns | 0.0062 |     104 B |
