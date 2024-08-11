```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method               | Length | Mean        | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|--------------------- |------- |------------:|----------:|----------:|-------:|-------:|----------:|
| **Rope&lt;char&gt;**           | **10**     |    **41.41 ns** |  **0.110 ns** |  **0.097 ns** | **0.0038** |      **-** |      **64 B** |
| string               | 10     |    10.79 ns |  0.097 ns |  0.086 ns | 0.0029 |      - |      48 B |
| ImmutableArray&lt;char&gt; | 10     |    20.60 ns |  0.175 ns |  0.163 ns | 0.0057 |      - |      96 B |
| **Rope&lt;char&gt;**           | **100**    |    **41.62 ns** |  **0.093 ns** |  **0.083 ns** | **0.0038** |      **-** |      **64 B** |
| string               | 100    |    62.63 ns |  0.298 ns |  0.264 ns | 0.0134 |      - |     224 B |
| ImmutableArray&lt;char&gt; | 100    |    33.66 ns |  0.461 ns |  0.409 ns | 0.0268 |      - |     448 B |
| **Rope&lt;char&gt;**           | **1000**   |    **41.71 ns** |  **0.079 ns** |  **0.074 ns** | **0.0038** |      **-** |      **64 B** |
| string               | 1000   |   586.66 ns |  5.482 ns |  4.578 ns | 0.1202 |      - |    2024 B |
| ImmutableArray&lt;char&gt; | 1000   |   127.31 ns |  2.566 ns |  4.883 ns | 0.2418 | 0.0007 |    4048 B |
| **Rope&lt;char&gt;**           | **10000**  |    **41.62 ns** |  **0.085 ns** |  **0.076 ns** | **0.0038** |      **-** |      **64 B** |
| string               | 10000  | 5,978.28 ns | 66.973 ns | 62.647 ns | 1.1902 |      - |   20024 B |
| ImmutableArray&lt;char&gt; | 10000  | 1,248.77 ns | 24.985 ns | 43.098 ns | 2.3880 | 0.0839 |   40048 B |
