```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.447 μs** | **3.798 μs** | **0.2082 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.744 μs | 9.924 μs | 0.5440 μs |  1.13 |    0.21 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.385 μs | 6.041 μs | 0.3312 μs |  1.39 |    0.16 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.758 μs | 5.756 μs | 0.3155 μs |  1.13 |    0.14 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 2.649 μs | 6.527 μs | 0.3578 μs |  1.09 |    0.15 |    1 |      64 B |        1.14 |
|                                          |            |          |          |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.416 μs** | **7.789 μs** | **0.4269 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 2.905 μs | 4.611 μs | 0.2527 μs |  0.86 |    0.11 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 4.730 μs | 4.180 μs | 0.2291 μs |  1.40 |    0.15 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 2.583 μs | 3.523 μs | 0.1931 μs |  0.76 |    0.09 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.486 μs | 6.335 μs | 0.3472 μs |  1.03 |    0.14 |    1 |      64 B |        1.14 |
