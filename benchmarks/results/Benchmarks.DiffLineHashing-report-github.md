```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method           | Mean     | Error   | StdDev  | Gen0       | Gen1       | Gen2      | Allocated |
|----------------- |---------:|--------:|--------:|-----------:|-----------:|----------:|----------:|
| LinesToCharsPure | 764.2 ms | 5.55 ms | 4.33 ms | 34000.0000 | 20000.0000 | 4000.0000 | 555.28 MB |
