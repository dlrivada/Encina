```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.218 μs** |  **5.0780 μs** | **0.2783 μs** |  **1.00** |    **0.11** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.192 μs |  7.8625 μs | 0.4310 μs |  1.00 |    0.14 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.280 μs | 10.8887 μs | 0.5968 μs |  1.34 |    0.19 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.278 μs |  7.0815 μs | 0.3882 μs |  1.02 |    0.13 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.440 μs |  6.8432 μs | 0.3751 μs |  1.07 |    0.13 |    1 |      64 B |        1.14 |
|                                          |            |          |            |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.140 μs** |  **7.4645 μs** | **0.4092 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.740 μs |  8.1863 μs | 0.4487 μs |  0.91 |    0.12 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.251 μs |  3.8972 μs | 0.2136 μs |  1.28 |    0.12 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.762 μs |  0.7595 μs | 0.0416 μs |  0.91 |    0.08 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.162 μs |  4.4126 μs | 0.2419 μs |  1.01 |    0.10 |    1 |      64 B |        1.14 |
