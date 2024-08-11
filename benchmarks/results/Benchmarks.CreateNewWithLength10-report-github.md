```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method                      | Mean      | Error     | StdDev    | Gen0   | Allocated |
|---------------------------- |----------:|----------:|----------:|-------:|----------:|
| string.ToRope()             |  5.868 ns | 0.1352 ns | 0.1328 ns | 0.0019 |      32 B |
| &#39;new Rope&lt;char&gt;(array)&#39;     |  6.728 ns | 0.1546 ns | 0.2361 ns | 0.0019 |      32 B |
| &#39;new List&lt;char&gt;(array)&#39;     | 17.077 ns | 0.3540 ns | 0.3934 ns | 0.0048 |      80 B |
| &#39;new StringBuilder(string)&#39; |  8.823 ns | 0.0246 ns | 0.0218 ns | 0.0062 |     104 B |
