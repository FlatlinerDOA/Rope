```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.302
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method                 | Mean        | Error     | StdDev    | Gen0      | Gen1      | Gen2     | Allocated   |
|----------------------- |------------:|----------:|----------:|----------:|----------:|---------:|------------:|
| AddRangeThenOrderLong  |    209.1 μs |   0.60 μs |   0.56 μs |         - |         - |        - |       184 B |
| InsertSortedLong       | 46,639.5 μs | 383.39 μs | 358.62 μs | 7000.0000 | 5909.0909 | 181.8182 | 118833769 B |
| PriorityQueueOfLong    |    551.9 μs |   9.61 μs |   8.99 μs |  506.8359 |  500.0000 | 499.0234 |   2097650 B |
| AddRangeThenOrderFloat |    205.2 μs |   0.36 μs |   0.34 μs |         - |         - |        - |       200 B |
| InsertSortedFloat      | 42,029.4 μs | 270.83 μs | 253.34 μs | 5750.0000 | 3916.6667 |        - |  97425609 B |
| PriorityQueueOfFloat   |    653.5 μs |  11.18 μs |  10.46 μs |  506.8359 |  500.0000 | 499.0234 |   2097654 B |
