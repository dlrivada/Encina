```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error       | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|------------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.046 μs** |   **3.4864 μs** | **0.1911 μs** | **2.936 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.097 μs |   4.5273 μs | 0.2482 μs | 3.187 μs |  1.02 |    0.09 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 7.460 μs | 108.9650 μs | 5.9727 μs | 4.037 μs |  2.46 |    1.71 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.489 μs |   3.5762 μs | 0.1960 μs | 3.486 μs |  1.15 |    0.08 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.597 μs |   8.9118 μs | 0.4885 μs | 3.426 μs |  1.18 |    0.15 |    1 |      64 B |        1.14 |
|                                          |            |          |             |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.409 μs** |   **2.7479 μs** | **0.1506 μs** | **3.416 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.596 μs |   2.4031 μs | 0.1317 μs | 3.646 μs |  1.06 |    0.05 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 4.903 μs |   7.5317 μs | 0.4128 μs | 4.790 μs |  1.44 |    0.12 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.509 μs |   0.5865 μs | 0.0321 μs | 3.496 μs |  1.03 |    0.04 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.890 μs |   0.9182 μs | 0.0503 μs | 3.897 μs |  1.14 |    0.05 |    1 |      64 B |        1.14 |
