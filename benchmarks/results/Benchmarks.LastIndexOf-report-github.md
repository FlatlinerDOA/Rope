```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method                                    | Mean       | Error     | StdDev    | Gen0   | Allocated |
|------------------------------------------ |-----------:|----------:|----------:|-------:|----------:|
| Rope&lt;char&gt;                                |   2.568 μs | 0.0514 μs | 0.0668 μs | 0.0038 |      80 B |
| Fragmented
Rope&lt;char&gt;                     |   3.938 μs | 0.0161 μs | 0.0151 μs | 0.0534 |     896 B |
| &#39;Fragmented
Rope&lt;char&gt; (Fragmented Find)&#39; |   3.382 μs | 0.0090 μs | 0.0084 μs | 0.0687 |    1168 B |
| String                                    | 455.932 μs | 1.8262 μs | 1.5250 μs |      - |         - |
