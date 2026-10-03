```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **1.429 μs** | **18.968 μs** | **1.0397 μs** | **1.0010 μs** |  **1.37** |    **1.18** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 1.446 μs | 20.376 μs | 1.1169 μs | 1.1020 μs |  1.38 |    1.24 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 2.036 μs | 28.054 μs | 1.5377 μs | 1.3620 μs |  1.95 |    1.72 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 1.549 μs | 21.841 μs | 1.1972 μs | 1.0520 μs |  1.48 |    1.33 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 2.841 μs | 14.182 μs | 0.7774 μs | 2.5340 μs |  2.72 |    1.54 |    3 |      64 B |        1.14 |
|                                          |            |          |           |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **2.033 μs** | **18.751 μs** | **1.0278 μs** | **1.6625 μs** |  **1.17** |    **0.69** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 1.726 μs | 17.530 μs | 0.9609 μs | 1.2010 μs |  0.99 |    0.63 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 3.123 μs | 23.299 μs | 1.2771 μs | 2.5785 μs |  1.79 |    0.95 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 1.811 μs | 18.778 μs | 1.0293 μs | 1.2470 μs |  1.04 |    0.67 |    2 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 1.699 μs | 24.114 μs | 1.3217 μs | 0.9720 μs |  0.97 |    0.79 |    1 |      64 B |        1.14 |
