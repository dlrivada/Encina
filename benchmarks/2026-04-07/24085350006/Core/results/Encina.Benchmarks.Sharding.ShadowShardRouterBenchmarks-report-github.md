```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.426 μs** |  **7.678 μs** | **0.4209 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.026 μs |  1.580 μs | 0.0866 μs |  0.89 |    0.10 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.618 μs |  6.500 μs | 0.3563 μs |  1.36 |    0.17 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.619 μs |  5.491 μs | 0.3010 μs |  1.07 |    0.14 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.274 μs |  1.951 μs | 0.1069 μs |  0.97 |    0.11 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.394 μs** |  **2.318 μs** | **0.1271 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 4.225 μs |  2.443 μs | 0.1339 μs |  1.25 |    0.05 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.707 μs | 18.734 μs | 1.0269 μs |  1.68 |    0.27 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.414 μs |  5.391 μs | 0.2955 μs |  1.01 |    0.08 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.718 μs |  3.758 μs | 0.2060 μs |  1.10 |    0.06 |    1 |      64 B |        1.14 |
