```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  19.19 μs |  13.78 μs |  0.755 μs |  1.00 |    0.05 |    2 |  0.7324 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  85.81 μs |  16.11 μs |  0.883 μs |  4.48 |    0.15 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 405.90 μs | 112.99 μs |  6.193 μs | 21.18 |    0.76 |    5 | 52.2461 | 52.2461 | 52.2461 | 219.51 KB |       18.25 |
| &#39;CSV export: 10 activities&#39;   |  10.01 μs |  10.08 μs |  0.552 μs |  0.52 |    0.03 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  40.18 μs |  64.38 μs |  3.529 μs |  2.10 |    0.17 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 341.98 μs | 392.73 μs | 21.527 μs | 17.84 |    1.14 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
