```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4317/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]     : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  Job-DEDBEB : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2

Runtime=.NET 8.0  Toolchain=net8.0  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|-------:|--------:|----------:|------------:|
| Rope&lt;char&gt;                                |   2.679 μs | 0.0509 μs | 0.0523 μs |   1.00 |    0.00 |         - |          NA |
| Fragmented
Rope&lt;char&gt;                     |   4.660 μs | 0.0141 μs | 0.0125 μs |   1.74 |    0.04 |         - |          NA |
| &#39;Fragmented
Rope&lt;char&gt; (Fragmented Find)&#39; |   4.458 μs | 0.0177 μs | 0.0166 μs |   1.66 |    0.03 |         - |          NA |
| String                                    | 483.925 μs | 8.3534 μs | 7.8138 μs | 180.64 |    5.13 |         - |          NA |
