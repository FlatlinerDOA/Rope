```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4317/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]     : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  Job-DEDBEB : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2

Runtime=.NET 8.0  Toolchain=net8.0  

```
| Method           | Mean     | Error   | StdDev  | Gen0       | Gen1       | Gen2      | Allocated |
|----------------- |---------:|--------:|--------:|-----------:|-----------:|----------:|----------:|
| LinesToCharsPure | 218.7 ms | 2.16 ms | 2.02 ms | 35333.3333 | 20333.3333 | 3000.0000 | 555.28 MB |
