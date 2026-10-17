```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean      | Error       | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |----------:|------------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          |  **3.094 μs** |   **1.8801 μs** |  **0.1031 μs** | **3.120 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          |  3.115 μs |   2.8498 μs |  0.1562 μs | 3.035 μs |  1.01 |    0.05 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 12.351 μs | 241.3907 μs | 13.2314 μs | 5.264 μs |  3.99 |    3.71 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          |  3.436 μs |   4.5044 μs |  0.2469 μs | 3.466 μs |  1.11 |    0.08 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          |  3.373 μs |   2.9575 μs |  0.1621 μs | 3.347 μs |  1.09 |    0.06 |    1 |      64 B |        1.14 |
|                                          |            |           |             |            |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         |  **3.446 μs** |   **1.3870 μs** |  **0.0760 μs** | **3.456 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         |  3.389 μs |   5.4852 μs |  0.3007 μs | 3.225 μs |  0.98 |    0.08 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         |  4.779 μs |   7.2506 μs |  0.3974 μs | 4.628 μs |  1.39 |    0.10 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         |  3.573 μs |   3.5851 μs |  0.1965 μs | 3.596 μs |  1.04 |    0.05 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         |  4.058 μs |   0.6578 μs |  0.0361 μs | 4.048 μs |  1.18 |    0.02 |    1 |      64 B |        1.14 |
