```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.534 μs** |  **2.554 μs** | **0.1400 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.482 μs |  4.493 μs | 0.2463 μs |  0.98 |    0.10 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.362 μs |  4.765 μs | 0.2612 μs |  1.33 |    0.11 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.751 μs |  7.355 μs | 0.4032 μs |  1.09 |    0.15 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 2.637 μs |  7.178 μs | 0.3935 μs |  1.04 |    0.14 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **2.921 μs** |  **6.539 μs** | **0.3584 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.222 μs |  7.359 μs | 0.4033 μs |  1.11 |    0.16 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.007 μs |  5.270 μs | 0.2889 μs |  1.73 |    0.19 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 2.980 μs |  8.090 μs | 0.4434 μs |  1.03 |    0.17 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.953 μs | 11.388 μs | 0.6242 μs |  1.37 |    0.23 |    2 |      64 B |        1.14 |
