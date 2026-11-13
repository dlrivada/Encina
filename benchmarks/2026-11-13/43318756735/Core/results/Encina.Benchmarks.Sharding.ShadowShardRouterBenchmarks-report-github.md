```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **5.422 μs** | **12.338 μs** | **0.6763 μs** |  **1.01** |    **0.16** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 4.912 μs | 30.298 μs | 1.6608 μs |  0.92 |    0.29 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 5.015 μs | 31.112 μs | 1.7054 μs |  0.94 |    0.30 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 5.954 μs | 16.081 μs | 0.8814 μs |  1.11 |    0.19 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.713 μs | 24.375 μs | 1.3361 μs |  0.69 |    0.23 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **5.544 μs** |  **7.700 μs** | **0.4221 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.637 μs | 13.690 μs | 0.7504 μs |  0.66 |    0.13 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.510 μs | 19.355 μs | 1.0609 μs |  1.00 |    0.18 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.296 μs | 12.722 μs | 0.6973 μs |  0.78 |    0.12 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 6.067 μs | 16.155 μs | 0.8855 μs |  1.10 |    0.16 |    2 |      64 B |        1.14 |
