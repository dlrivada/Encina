```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.416 μs** | **30.426 μs** | **1.6677 μs** | **2.151 μs** |  **1.47** |    **1.40** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.865 μs | 33.184 μs | 1.8189 μs | 4.590 μs |  2.34 |    1.89 |    3 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 6.003 μs | 39.091 μs | 2.1427 μs | 7.058 μs |  3.64 |    2.71 |    4 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.786 μs | 39.822 μs | 2.1828 μs | 1.550 μs |  1.69 |    1.74 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.605 μs | 24.018 μs | 1.3165 μs | 3.930 μs |  2.19 |    1.64 |    3 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.900 μs** |  **7.337 μs** | **0.4021 μs** | **3.671 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 4.077 μs | 20.377 μs | 1.1170 μs | 3.680 μs |  1.05 |    0.27 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 7.203 μs | 30.186 μs | 1.6546 μs | 7.224 μs |  1.86 |    0.40 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.871 μs |  8.443 μs | 0.4628 μs | 4.818 μs |  1.26 |    0.15 |    2 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 6.263 μs | 20.957 μs | 1.1487 μs | 6.801 μs |  1.62 |    0.29 |    3 |      64 B |        1.14 |
