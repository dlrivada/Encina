```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.778 μs** |   **7.453 μs** |  **0.4085 μs** | **2.685 μs** |  **1.01** |    **0.18** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.542 μs |   5.849 μs |  0.3206 μs | 2.398 μs |  0.93 |    0.15 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.529 μs |   5.248 μs |  0.2877 μs | 3.455 μs |  1.29 |    0.18 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 8.732 μs | 195.432 μs | 10.7123 μs | 2.673 μs |  3.19 |    3.43 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.038 μs |   6.459 μs |  0.3540 μs | 2.865 μs |  1.11 |    0.18 |    1 |      64 B |        1.14 |
|                                          |            |          |            |            |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.198 μs** |  **13.943 μs** |  **0.7643 μs** | **2.904 μs** |  **1.04** |    **0.29** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.247 μs |  10.150 μs |  0.5564 μs | 3.224 μs |  1.05 |    0.25 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 4.093 μs |   7.662 μs |  0.4200 μs | 3.866 μs |  1.33 |    0.27 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 2.777 μs |   2.566 μs |  0.1406 μs | 2.784 μs |  0.90 |    0.17 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.754 μs |  10.345 μs |  0.5670 μs | 4.437 μs |  1.54 |    0.33 |    2 |      64 B |        1.14 |
