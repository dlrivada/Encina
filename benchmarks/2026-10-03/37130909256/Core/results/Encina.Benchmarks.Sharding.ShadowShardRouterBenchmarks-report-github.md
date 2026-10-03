```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **1.977 μs** | **33.529 μs** | **1.8379 μs** | **1.0120 μs** |  **1.61** |    **1.70** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.544 μs |  4.655 μs | 0.2551 μs | 2.4440 μs |  2.08 |    1.14 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 1.958 μs | 27.311 μs | 1.4970 μs | 1.2165 μs |  1.60 |    1.47 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 1.457 μs | 21.546 μs | 1.1810 μs | 0.9960 μs |  1.19 |    1.14 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 2.307 μs | 26.803 μs | 1.4692 μs | 2.4740 μs |  1.88 |    1.55 |    2 |      64 B |        1.14 |
|                                          |            |          |           |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **1.788 μs** | **16.083 μs** | **0.8816 μs** | **1.3770 μs** |  **1.15** |    **0.65** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 1.559 μs | 18.663 μs | 1.0230 μs | 1.1420 μs |  1.00 |    0.69 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 2.377 μs | 28.614 μs | 1.5684 μs | 1.7225 μs |  1.53 |    1.05 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 1.683 μs | 17.297 μs | 0.9481 μs | 1.2120 μs |  1.08 |    0.67 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 2.080 μs | 20.215 μs | 1.1081 μs | 1.7130 μs |  1.34 |    0.79 |    2 |      64 B |        1.14 |
