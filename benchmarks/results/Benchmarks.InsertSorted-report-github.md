```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4317/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]     : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  Job-QIWBTM : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2

Runtime=.NET 8.0  Toolchain=net8.0  

```
| Method                 | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|----------------------- |---------:|--------:|--------:|------:|----------:|------------:|
| AddRangeThenOrderLong  | 279.8 μs | 1.33 μs | 1.24 μs |  1.00 |     168 B |        1.00 |
| AddRangeThenOrderFloat | 264.4 μs | 0.22 μs | 0.19 μs |  0.94 |     184 B |        1.10 |
