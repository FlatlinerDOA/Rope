```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method             | Mean      | Error    | StdDev   | Gen0     | Gen1     | Allocated |
|------------------- |----------:|---------:|---------:|---------:|---------:|----------:|
| RopeOfCharDiff     | 104.89 ms | 0.235 ms | 0.220 ms | 800.0000 | 200.0000 |  13.27 MB |
| DiffMatchPatchDiff |  50.55 ms | 0.159 ms | 0.148 ms | 300.0000 | 100.0000 |   4.96 MB |
