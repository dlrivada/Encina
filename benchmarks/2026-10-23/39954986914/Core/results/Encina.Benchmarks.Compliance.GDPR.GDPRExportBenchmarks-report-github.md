```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|------------:|-----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  17.654 μs |   4.7685 μs |  0.2614 μs |  1.00 |    0.02 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  80.441 μs |   9.2560 μs |  0.5074 μs |  4.56 |    0.06 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 412.071 μs | 103.9636 μs |  5.6986 μs | 23.35 |    0.41 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |   7.955 μs |   0.8109 μs |  0.0444 μs |  0.45 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  34.680 μs |   7.0475 μs |  0.3863 μs |  1.96 |    0.03 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 248.271 μs | 266.8936 μs | 14.6293 μs | 14.07 |    0.74 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
