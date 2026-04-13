```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.960 μs** | **0.1517 μs** | **0.2270 μs** | **2.931 μs** |  **1.01** |    **0.10** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.229 μs | 0.1882 μs | 0.2700 μs | 3.126 μs |  1.10 |    0.12 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.088 μs | 0.2550 μs | 0.3737 μs | 3.913 μs |  1.39 |    0.16 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.647 μs | 0.2269 μs | 0.3396 μs | 3.762 μs |  1.24 |    0.14 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.498 μs | 0.2459 μs | 0.3605 μs | 3.627 μs |  1.19 |    0.15 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.675 μs** | **0.3186 μs** | **0.4466 μs** | **3.596 μs** |  **1.01** |    **0.17** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.309 μs | 0.0861 μs | 0.1289 μs | 3.307 μs |  0.91 |    0.11 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.081 μs | 0.5259 μs | 0.7021 μs | 4.559 μs |  1.40 |    0.25 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.819 μs | 0.3450 μs | 0.4947 μs | 3.770 μs |  1.05 |    0.18 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.894 μs | 0.1020 μs | 0.1463 μs | 3.862 μs |  1.07 |    0.13 |    1 |      64 B |        1.14 |
