```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.302 μs** |  **6.314 μs** | **0.3461 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.660 μs |  9.668 μs | 0.5299 μs |  1.12 |    0.17 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 5.741 μs | 37.425 μs | 2.0514 μs |  1.75 |    0.57 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.622 μs |  6.200 μs | 0.3398 μs |  1.10 |    0.14 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.703 μs |  8.149 μs | 0.4467 μs |  1.13 |    0.16 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.989 μs** | **15.519 μs** | **0.8507 μs** |  **1.02** |    **0.20** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 5.089 μs | 19.963 μs | 1.0942 μs |  1.04 |    0.24 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.902 μs |  6.905 μs | 0.3785 μs |  1.20 |    0.18 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.706 μs |  8.408 μs | 0.4609 μs |  0.96 |    0.15 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.369 μs |  8.399 μs | 0.4604 μs |  0.89 |    0.15 |    1 |      64 B |        1.14 |
