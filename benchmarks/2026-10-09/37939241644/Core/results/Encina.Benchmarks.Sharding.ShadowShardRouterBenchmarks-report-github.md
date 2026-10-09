```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.067 μs** | **13.709 μs** | **0.7514 μs** | **2.689 μs** |  **1.04** |    **0.30** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.103 μs | 15.461 μs | 0.8475 μs | 2.863 μs |  1.05 |    0.32 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.872 μs | 28.731 μs | 1.5748 μs | 3.034 μs |  1.31 |    0.53 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 1.665 μs | 22.759 μs | 1.2475 μs | 1.302 μs |  0.56 |    0.39 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.108 μs | 19.604 μs | 1.0746 μs | 2.644 μs |  1.05 |    0.37 |    2 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **2.380 μs** | **22.848 μs** | **1.2524 μs** | **1.812 μs** |  **1.17** |    **0.71** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 2.137 μs | 19.284 μs | 1.0570 μs | 1.823 μs |  1.05 |    0.61 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 3.577 μs | 36.465 μs | 1.9988 μs | 2.869 μs |  1.76 |    1.10 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 2.418 μs | 26.468 μs | 1.4508 μs | 1.667 μs |  1.19 |    0.78 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 2.632 μs | 35.149 μs | 1.9266 μs | 1.878 μs |  1.29 |    0.99 |    1 |      64 B |        1.14 |
