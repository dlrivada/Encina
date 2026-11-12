```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          |  **3.132 μs** |   **1.005 μs** |  **0.0551 μs** | **3.136 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          |  3.730 μs |   2.463 μs |  0.1350 μs | 3.727 μs |  1.19 |    0.04 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          |  5.855 μs |  18.124 μs |  0.9934 μs | 5.411 μs |  1.87 |    0.28 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          |  3.290 μs |   2.012 μs |  0.1103 μs | 3.226 μs |  1.05 |    0.03 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 10.043 μs | 185.633 μs | 10.1752 μs | 4.299 μs |  3.21 |    2.81 |    3 |      64 B |        1.14 |
|                                          |            |           |            |            |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         |  **3.788 μs** |   **3.344 μs** |  **0.1833 μs** | **3.828 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         |  4.077 μs |   6.638 μs |  0.3639 μs | 4.277 μs |  1.08 |    0.10 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         |  5.478 μs |   7.871 μs |  0.4314 μs | 5.255 μs |  1.45 |    0.12 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         |  3.607 μs |   3.014 μs |  0.1652 μs | 3.517 μs |  0.95 |    0.06 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         |  4.459 μs |   1.651 μs |  0.0905 μs | 4.459 μs |  1.18 |    0.05 |    1 |      64 B |        1.14 |
