```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.126 μs** | **2.276 μs** | **0.1247 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.112 μs | 1.375 μs | 0.0753 μs |  1.00 |    0.04 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.056 μs | 2.811 μs | 0.1541 μs |  1.30 |    0.06 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.333 μs | 1.196 μs | 0.0656 μs |  1.07 |    0.04 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.346 μs | 3.641 μs | 0.1996 μs |  1.07 |    0.07 |    1 |      64 B |        1.14 |
|                                          |            |          |          |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.567 μs** | **5.213 μs** | **0.2858 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.440 μs | 2.534 μs | 0.1389 μs |  0.97 |    0.07 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 4.819 μs | 2.249 μs | 0.1232 μs |  1.36 |    0.10 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.474 μs | 2.010 μs | 0.1102 μs |  0.98 |    0.07 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.847 μs | 2.337 μs | 0.1281 μs |  1.08 |    0.08 |    1 |      64 B |        1.14 |
