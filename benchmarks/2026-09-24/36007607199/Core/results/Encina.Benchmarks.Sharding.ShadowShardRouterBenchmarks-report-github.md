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
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.823 μs** | **8.913 μs** | **0.4886 μs** |  **1.01** |    **0.16** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.013 μs | 5.087 μs | 0.2789 μs |  0.80 |    0.11 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.371 μs | 6.284 μs | 0.3444 μs |  1.16 |    0.15 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 4.163 μs | 8.444 μs | 0.4628 μs |  1.10 |    0.16 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.882 μs | 6.854 μs | 0.3757 μs |  1.03 |    0.15 |    2 |      64 B |        1.14 |
|                                          |            |          |          |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.423 μs** | **3.109 μs** | **0.1704 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.434 μs | 5.521 μs | 0.3026 μs |  1.00 |    0.09 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.794 μs | 7.256 μs | 0.3977 μs |  1.70 |    0.12 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.346 μs | 5.401 μs | 0.2960 μs |  0.98 |    0.09 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.244 μs | 5.378 μs | 0.2948 μs |  1.24 |    0.09 |    2 |      64 B |        1.14 |
