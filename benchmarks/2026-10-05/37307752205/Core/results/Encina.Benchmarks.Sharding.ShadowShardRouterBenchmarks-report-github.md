```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.888 μs** | **28.895 μs** | **1.5839 μs** | **2.370 μs** |  **1.20** |    **0.79** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.782 μs | 26.807 μs | 1.4694 μs | 2.109 μs |  1.16 |    0.75 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.910 μs | 40.710 μs | 2.2315 μs | 2.711 μs |  1.63 |    1.10 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 4.455 μs | 22.793 μs | 1.2493 μs | 3.961 μs |  1.86 |    0.91 |    3 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.468 μs | 31.490 μs | 1.7261 μs | 2.930 μs |  1.45 |    0.90 |    2 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **5.850 μs** | **17.304 μs** | **0.9485 μs** | **5.621 μs** |  **1.02** |    **0.20** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 4.173 μs | 17.598 μs | 0.9646 μs | 3.831 μs |  0.73 |    0.18 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 7.253 μs | 12.730 μs | 0.6978 μs | 7.553 μs |  1.26 |    0.20 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.928 μs | 17.615 μs | 0.9655 μs | 3.723 μs |  0.68 |    0.17 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.556 μs | 21.405 μs | 1.1733 μs | 4.197 μs |  0.79 |    0.21 |    1 |      64 B |        1.14 |
