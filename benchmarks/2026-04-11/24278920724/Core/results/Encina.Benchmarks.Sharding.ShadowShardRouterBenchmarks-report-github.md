```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.065 μs** | **0.0435 μs** | **0.0596 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.005 μs | 0.1130 μs | 0.1547 μs |  0.98 |    0.05 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.513 μs | 0.2873 μs | 0.4027 μs |  1.47 |    0.13 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.206 μs | 0.0876 μs | 0.1228 μs |  1.05 |    0.04 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.425 μs | 0.1820 μs | 0.2667 μs |  1.12 |    0.09 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.414 μs** | **0.0630 μs** | **0.0883 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.388 μs | 0.0727 μs | 0.0995 μs |  0.99 |    0.04 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 4.610 μs | 0.0882 μs | 0.1236 μs |  1.35 |    0.05 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.749 μs | 0.3320 μs | 0.4544 μs |  1.10 |    0.13 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.954 μs | 0.0629 μs | 0.0902 μs |  1.16 |    0.04 |    1 |      64 B |        1.14 |
