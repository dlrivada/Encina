```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          |  **3.381 μs** |   **8.711 μs** |  **0.4775 μs** | **3.110 μs** |  **1.01** |    **0.17** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          |  3.586 μs |   8.433 μs |  0.4623 μs | 3.746 μs |  1.07 |    0.17 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          |  4.348 μs |   6.172 μs |  0.3383 μs | 4.398 μs |  1.30 |    0.17 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          |  3.209 μs |   1.943 μs |  0.1065 μs | 3.226 μs |  0.96 |    0.11 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          |  3.479 μs |   7.054 μs |  0.3867 μs | 3.335 μs |  1.04 |    0.16 |    1 |      64 B |        1.14 |
|                                          |            |           |            |            |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         |  **3.480 μs** |   **2.645 μs** |  **0.1450 μs** | **3.407 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         |  3.510 μs |   2.945 μs |  0.1614 μs | 3.497 μs |  1.01 |    0.05 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         |  5.337 μs |   2.763 μs |  0.1514 μs | 5.270 μs |  1.54 |    0.07 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         |  3.329 μs |   4.333 μs |  0.2375 μs | 3.201 μs |  0.96 |    0.07 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 11.689 μs | 242.712 μs | 13.3039 μs | 4.008 μs |  3.36 |    3.32 |    3 |      64 B |        1.14 |
