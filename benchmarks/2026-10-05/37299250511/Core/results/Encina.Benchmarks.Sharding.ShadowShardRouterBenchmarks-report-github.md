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
| **&#39;Bare HashRouter&#39;**                        | **3**          | **11.174 μs** | **247.669 μs** | **13.5756 μs** | **3.366 μs** |  **2.37** |    **3.24** |    **3** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          |  3.183 μs |   1.562 μs |  0.0856 μs | 3.216 μs |  0.68 |    0.42 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          |  4.757 μs |  11.620 μs |  0.6369 μs | 5.094 μs |  1.01 |    0.64 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          |  3.563 μs |   9.537 μs |  0.5227 μs | 3.466 μs |  0.76 |    0.48 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          |  3.691 μs |   7.874 μs |  0.4316 μs | 3.607 μs |  0.78 |    0.49 |    1 |      64 B |        1.14 |
|                                          |            |           |            |            |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         |  **3.500 μs** |   **4.858 μs** |  **0.2663 μs** | **3.476 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         |  3.456 μs |   3.854 μs |  0.2112 μs | 3.477 μs |  0.99 |    0.08 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         |  4.855 μs |   5.155 μs |  0.2825 μs | 4.729 μs |  1.39 |    0.12 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         |  3.535 μs |   2.658 μs |  0.1457 μs | 3.471 μs |  1.01 |    0.08 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         |  3.976 μs |   1.968 μs |  0.1079 μs | 4.023 μs |  1.14 |    0.08 |    1 |      64 B |        1.14 |
