```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4317/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]     : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  Job-DEDBEB : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2

Runtime=.NET 8.0  Toolchain=net8.0  

```
| Method             | Mean     | Error    | StdDev   | Ratio | Gen0     | Gen1     | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|------:|---------:|---------:|----------:|------------:|
| RopeOfCharDiff     | 62.04 ms | 0.208 ms | 0.195 ms |  1.00 | 777.7778 | 333.3333 |  12.87 MB |        1.00 |
| DiffMatchPatchDiff | 50.47 ms | 0.062 ms | 0.058 ms |  0.81 | 300.0000 | 100.0000 |   4.96 MB |        0.39 |
