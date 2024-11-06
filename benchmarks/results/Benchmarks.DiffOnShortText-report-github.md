```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4317/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]     : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  Job-DEDBEB : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2

Runtime=.NET 8.0  Toolchain=net8.0  

```
| Method             | Mean     | Error    | StdDev   | Gen0     | Gen1    | Allocated |
|------------------- |---------:|---------:|---------:|---------:|--------:|----------:|
| RopeDiff           | 16.57 ms | 0.028 ms | 0.024 ms | 250.0000 | 31.2500 |   4.49 MB |
| DiffMatchPatchDiff | 11.16 ms | 0.024 ms | 0.022 ms |  78.1250 | 15.6250 |   1.39 MB |
