```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|------------:|-----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  16.980 μs |   0.1989 μs |  0.0109 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  80.566 μs |  15.0234 μs |  0.8235 μs |  4.74 |    0.04 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 390.989 μs |  13.3698 μs |  0.7328 μs | 23.03 |    0.04 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |   7.398 μs |   1.4508 μs |  0.0795 μs |  0.44 |    0.00 |    1 |  3.3569 |  0.2365 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  32.808 μs |   4.6479 μs |  0.2548 μs |  1.93 |    0.01 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 223.317 μs | 197.8537 μs | 10.8450 μs | 13.15 |    0.55 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
