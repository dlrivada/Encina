```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  24.79 μs |   3.160 μs |  0.173 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 114.98 μs |   7.250 μs |  0.397 μs |  4.64 |    0.03 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 545.80 μs |  41.782 μs |  2.290 μs | 22.02 |    0.16 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  11.66 μs |   5.853 μs |  0.321 μs |  0.47 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  50.67 μs |  21.173 μs |  1.161 μs |  2.04 |    0.04 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 333.46 μs | 221.528 μs | 12.143 μs | 13.45 |    0.43 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
