```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method                      | Mean     | Error     | StdDev    | Gen0   | Allocated |
|---------------------------- |---------:|----------:|----------:|-------:|----------:|
| CommonPrefixLengthLargeFind | 1.025 μs | 0.0052 μs | 0.0048 μs | 0.0992 |   1.63 KB |
| IndexOfLargeFind            | 1.720 μs | 0.0082 μs | 0.0068 μs | 0.0992 |   1.63 KB |
| LastIndexOfLargeFind        | 1.815 μs | 0.0060 μs | 0.0056 μs | 0.0992 |   1.63 KB |
