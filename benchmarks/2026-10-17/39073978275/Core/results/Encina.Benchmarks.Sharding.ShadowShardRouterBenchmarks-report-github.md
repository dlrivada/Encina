```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          |  **3.588 μs** |   **6.079 μs** |  **0.3332 μs** | **3.641 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          |  3.079 μs |   2.935 μs |  0.1609 μs | 3.067 μs |  0.86 |    0.08 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          |  5.113 μs |   5.863 μs |  0.3214 μs | 5.140 μs |  1.43 |    0.14 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          |  3.259 μs |   4.906 μs |  0.2689 μs | 3.146 μs |  0.91 |    0.10 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          |  3.503 μs |   7.330 μs |  0.4018 μs | 3.285 μs |  0.98 |    0.13 |    1 |      64 B |        1.14 |
|                                          |            |           |            |            |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         |  **3.489 μs** |   **5.833 μs** |  **0.3197 μs** | **3.426 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         |  3.815 μs |   9.135 μs |  0.5007 μs | 3.701 μs |  1.10 |    0.15 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         |  4.737 μs |   7.575 μs |  0.4152 μs | 4.534 μs |  1.36 |    0.15 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         |  3.422 μs |   6.554 μs |  0.3593 μs | 3.225 μs |  0.99 |    0.12 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 12.278 μs | 264.516 μs | 14.4990 μs | 4.093 μs |  3.54 |    3.64 |    2 |      64 B |        1.14 |
