```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.046 μs** |   **1.431 μs** | **0.0784 μs** | **3.086 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.067 μs |   2.593 μs | 0.1421 μs | 2.990 μs |  1.01 |    0.05 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.067 μs |   3.665 μs | 0.2009 μs | 3.966 μs |  1.34 |    0.06 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.152 μs |   3.865 μs | 0.2118 μs | 3.046 μs |  1.04 |    0.06 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.272 μs |   3.809 μs | 0.2088 μs | 3.171 μs |  1.07 |    0.06 |    1 |      64 B |        1.14 |
|                                          |            |          |            |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **8.478 μs** | **163.409 μs** | **8.9570 μs** | **3.321 μs** |  **1.86** |    **2.20** |    **3** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.343 μs |   2.852 μs | 0.1563 μs | 3.366 μs |  0.73 |    0.42 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.590 μs |   5.235 μs | 0.2870 μs | 5.500 μs |  1.23 |    0.70 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.476 μs |   1.770 μs | 0.0970 μs | 3.516 μs |  0.76 |    0.43 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.895 μs |   7.216 μs | 0.3955 μs | 3.708 μs |  0.85 |    0.49 |    1 |      64 B |        1.14 |
