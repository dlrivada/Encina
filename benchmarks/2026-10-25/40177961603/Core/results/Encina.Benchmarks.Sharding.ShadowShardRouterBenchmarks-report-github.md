```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.582 μs** | **14.712 μs** | **0.8064 μs** | **2.258 μs** |  **1.06** |    **0.39** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.026 μs | 16.994 μs | 0.9315 μs | 2.544 μs |  0.83 |    0.39 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.400 μs | 19.968 μs | 1.0945 μs | 2.919 μs |  1.40 |    0.52 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 1.554 μs | 18.825 μs | 1.0319 μs | 1.186 μs |  0.64 |    0.41 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.004 μs | 14.132 μs | 0.7746 μs | 2.744 μs |  1.23 |    0.41 |    2 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **1.632 μs** | **18.679 μs** | **1.0238 μs** | **1.081 μs** |  **1.24** |    **0.88** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 1.928 μs | 17.996 μs | 0.9864 μs | 1.537 μs |  1.46 |    0.91 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 2.626 μs | 22.689 μs | 1.2437 μs | 1.948 μs |  1.99 |    1.19 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 1.739 μs | 18.573 μs | 1.0180 μs | 1.182 μs |  1.32 |    0.89 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 1.956 μs | 21.315 μs | 1.1684 μs | 1.312 μs |  1.49 |    1.02 |    1 |      64 B |        1.14 |
