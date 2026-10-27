```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          |  **3.152 μs** |   **1.166 μs** |  **0.0639 μs** | **3.125 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          |  3.183 μs |   2.454 μs |  0.1345 μs | 3.187 μs |  1.01 |    0.04 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          |  4.075 μs |   3.214 μs |  0.1762 μs | 3.978 μs |  1.29 |    0.05 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 11.804 μs | 265.350 μs | 14.5448 μs | 3.472 μs |  3.75 |    4.00 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          |  3.363 μs |   1.571 μs |  0.0861 μs | 3.347 μs |  1.07 |    0.03 |    1 |      64 B |        1.14 |
|                                          |            |           |            |            |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         |  **3.647 μs** |   **3.330 μs** |  **0.1825 μs** | **3.547 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         |  4.285 μs |   7.561 μs |  0.4144 μs | 4.189 μs |  1.18 |    0.11 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         |  4.766 μs |   3.976 μs |  0.2179 μs | 4.699 μs |  1.31 |    0.08 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         |  3.380 μs |   1.469 μs |  0.0805 μs | 3.426 μs |  0.93 |    0.04 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         |  4.011 μs |  10.805 μs |  0.5923 μs | 3.747 μs |  1.10 |    0.15 |    1 |      64 B |        1.14 |
