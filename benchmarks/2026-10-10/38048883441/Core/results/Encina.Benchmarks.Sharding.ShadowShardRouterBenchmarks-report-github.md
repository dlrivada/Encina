```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev   | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **1.581 μs** | **22.025 μs** | **1.207 μs** | **1.111 μs** |  **1.43** |    **1.31** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 1.726 μs | 25.783 μs | 1.413 μs | 1.091 μs |  1.56 |    1.50 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.262 μs | 33.451 μs | 1.834 μs | 2.745 μs |  2.95 |    2.27 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 1.883 μs | 28.175 μs | 1.544 μs | 1.162 μs |  1.70 |    1.64 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 2.125 μs | 35.061 μs | 1.922 μs | 1.297 μs |  1.92 |    1.98 |    1 |      64 B |        1.14 |
|                                          |            |          |           |          |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **2.988 μs** | **29.160 μs** | **1.598 μs** | **2.193 μs** |  **1.17** |    **0.71** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 2.092 μs | 22.181 μs | 1.216 μs | 1.578 μs |  0.82 |    0.53 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 3.447 μs | 46.471 μs | 2.547 μs | 2.308 μs |  1.35 |    1.04 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 2.542 μs | 36.491 μs | 2.000 μs | 1.578 μs |  1.00 |    0.80 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.090 μs | 49.476 μs | 2.712 μs | 3.215 μs |  1.61 |    1.13 |    3 |      64 B |        1.14 |
