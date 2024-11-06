```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4317/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]     : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  Job-DEDBEB : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2

Runtime=.NET 8.0  Toolchain=net8.0  

```
| Method                      | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| string.ToRope()             |  5.683 ns | 0.0263 ns | 0.0233 ns |  1.00 | 0.0019 |      32 B |        1.00 |
| &#39;new Rope&lt;char&gt;(array)&#39;     |  6.152 ns | 0.0342 ns | 0.0320 ns |  1.08 | 0.0019 |      32 B |        1.00 |
| &#39;new List&lt;char&gt;(array)&#39;     | 15.100 ns | 0.1010 ns | 0.0945 ns |  2.65 | 0.0048 |      80 B |        2.50 |
| &#39;new StringBuilder(string)&#39; |  8.603 ns | 0.0502 ns | 0.0469 ns |  1.51 | 0.0062 |     104 B |        3.25 |
