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
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.262 μs** |  **1.076 μs** | **0.0590 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.123 μs |  1.485 μs | 0.0814 μs |  0.96 |    0.03 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.195 μs |  4.907 μs | 0.2690 μs |  1.29 |    0.07 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.594 μs |  9.243 μs | 0.5067 μs |  1.10 |    0.14 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.420 μs |  2.690 μs | 0.1474 μs |  1.05 |    0.04 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.472 μs** |  **3.421 μs** | **0.1875 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.782 μs |  3.570 μs | 0.1957 μs |  1.09 |    0.07 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.614 μs |  2.299 μs | 0.1260 μs |  1.62 |    0.08 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.568 μs |  2.946 μs | 0.1615 μs |  1.32 |    0.07 |    2 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.668 μs | 17.543 μs | 0.9616 μs |  1.35 |    0.25 |    2 |      64 B |        1.14 |
