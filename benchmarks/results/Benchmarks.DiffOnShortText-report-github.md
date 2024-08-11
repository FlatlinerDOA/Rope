```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method             | Mean     | Error    | StdDev   | Gen0     | Gen1    | Allocated |
|------------------- |---------:|---------:|---------:|---------:|--------:|----------:|
| RopeDiff           | 16.20 ms | 0.048 ms | 0.045 ms | 281.2500 | 31.2500 |   4.64 MB |
| DiffMatchPatchDiff | 11.19 ms | 0.043 ms | 0.041 ms |  78.1250 | 15.6250 |   1.39 MB |
