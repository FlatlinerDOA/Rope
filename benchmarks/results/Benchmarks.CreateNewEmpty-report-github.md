```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method                | Mean      | Error     | StdDev    | Gen0   | Allocated |
|---------------------- |----------:|----------:|----------:|-------:|----------:|
| Rope&lt;char&gt;.Empty      | 0.0000 ns | 0.0000 ns | 0.0000 ns |      - |         - |
| &#39;new List&lt;char&gt;()&#39;    | 2.0997 ns | 0.0468 ns | 0.0415 ns | 0.0019 |      32 B |
| &#39;new StringBuilder()&#39; | 6.3449 ns | 0.0760 ns | 0.0634 ns | 0.0062 |     104 B |
