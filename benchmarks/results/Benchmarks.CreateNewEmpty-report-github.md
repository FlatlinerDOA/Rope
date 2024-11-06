```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4317/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]     : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  Job-DEDBEB : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2

Runtime=.NET 8.0  Toolchain=net8.0  

```
| Method                | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Rope&lt;char&gt;.Empty      | 0.0002 ns | 0.0003 ns | 0.0003 ns | 0.0000 ns |     ? |       ? |      - |         - |           ? |
| &#39;new List&lt;char&gt;()&#39;    | 2.1275 ns | 0.0368 ns | 0.0345 ns | 2.1383 ns |     ? |       ? | 0.0019 |      32 B |           ? |
| &#39;new StringBuilder()&#39; | 6.4693 ns | 0.0719 ns | 0.0637 ns | 6.4806 ns |     ? |       ? | 0.0062 |     104 B |           ? |
