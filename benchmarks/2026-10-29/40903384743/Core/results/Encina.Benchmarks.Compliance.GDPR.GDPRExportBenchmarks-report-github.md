```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  18.705 μs |   1.646 μs | 0.0902 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  90.681 μs |  29.559 μs | 1.6202 μs |  4.85 |    0.08 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 434.738 μs |  84.771 μs | 4.6466 μs | 23.24 |    0.24 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |   8.172 μs |   2.659 μs | 0.1458 μs |  0.44 |    0.01 |    1 |  3.3569 |  0.2365 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  36.298 μs |   2.865 μs | 0.1570 μs |  1.94 |    0.01 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 251.925 μs | 104.568 μs | 5.7317 μs | 13.47 |    0.27 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
