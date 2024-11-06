```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method        | Length | Mean       | Error     | StdDev    | Allocated |
|-------------- |------- |-----------:|----------:|----------:|----------:|
| **Rope&lt;char&gt;**    | **10**     |   **9.539 ns** | **0.2055 ns** | **0.2673 ns** |         **-** |
| StringBuilder | 10     |   7.912 ns | 0.0335 ns | 0.0314 ns |         - |
| string        | 10     |   2.587 ns | 0.0246 ns | 0.0230 ns |         - |
| **Rope&lt;char&gt;**    | **100**    |   **9.413 ns** | **0.1150 ns** | **0.1020 ns** |         **-** |
| StringBuilder | 100    |  10.198 ns | 0.0143 ns | 0.0120 ns |         - |
| string        | 100    |   4.099 ns | 0.0342 ns | 0.0320 ns |         - |
| **Rope&lt;char&gt;**    | **1000**   |   **9.212 ns** | **0.0903 ns** | **0.0800 ns** |         **-** |
| StringBuilder | 1000   |  31.699 ns | 0.1249 ns | 0.1107 ns |         - |
| string        | 1000   |  25.962 ns | 0.0646 ns | 0.0605 ns |         - |
| **Rope&lt;char&gt;**    | **10000**  |   **9.225 ns** | **0.2058 ns** | **0.3712 ns** |         **-** |
| StringBuilder | 10000  | 266.645 ns | 1.0231 ns | 0.9570 ns |         - |
| string        | 10000  | 265.631 ns | 1.0321 ns | 0.9654 ns |         - |
