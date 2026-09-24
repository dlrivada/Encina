```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  36.68 μs |   1.664 μs |  0.091 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 173.28 μs |   3.281 μs |  0.180 μs |  4.72 |    0.01 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 844.62 μs |  60.440 μs |  3.313 μs | 23.02 |    0.09 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.28 |
| &#39;CSV export: 10 activities&#39;   |  17.83 μs |   5.350 μs |  0.293 μs |  0.49 |    0.01 |    1 |  3.3569 |  0.2136 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  76.75 μs |  15.681 μs |  0.860 μs |  2.09 |    0.02 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 519.04 μs | 513.510 μs | 28.147 μs | 14.15 |    0.67 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
