```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  34.98 μs |  0.280 μs |  0.392 μs |  34.71 μs |  1.00 |    0.02 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 170.16 μs |  0.491 μs |  0.720 μs | 170.48 μs |  4.87 |    0.06 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 767.89 μs |  8.489 μs | 11.900 μs | 770.55 μs | 21.96 |    0.41 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  15.68 μs |  0.157 μs |  0.230 μs |  15.69 μs |  0.45 |    0.01 |    1 |  3.3569 |  0.2136 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  70.78 μs |  0.494 μs |  0.739 μs |  70.68 μs |  2.02 |    0.03 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 421.20 μs | 10.022 μs | 14.373 μs | 418.48 μs | 12.04 |    0.42 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
