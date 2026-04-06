```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **4.718 μs** | **22.990 μs** | **1.2601 μs** |  **1.04** |    **0.32** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.380 μs |  8.596 μs | 0.4712 μs |  0.75 |    0.18 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.563 μs | 10.519 μs | 0.5766 μs |  1.01 |    0.23 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.954 μs | 11.366 μs | 0.6230 μs |  0.87 |    0.21 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.641 μs | 10.316 μs | 0.5655 μs |  0.80 |    0.20 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **10**         | **5.388 μs** |  **9.819 μs** | **0.5382 μs** |  **1.01** |    **0.12** |    **3** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 10         | 3.341 μs | 12.176 μs | 0.6674 μs |  0.62 |    0.12 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 10         | 4.936 μs | 13.699 μs | 0.7509 μs |  0.92 |    0.14 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 10         | 3.417 μs | 10.575 μs | 0.5797 μs |  0.64 |    0.11 |    1 |     160 B |        2.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 10         | 4.443 μs | 27.388 μs | 1.5012 μs |  0.83 |    0.25 |    2 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.959 μs** | **10.626 μs** | **0.5824 μs** |  **1.01** |    **0.18** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 4.851 μs | 11.614 μs | 0.6366 μs |  1.24 |    0.21 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.182 μs | 20.528 μs | 1.1252 μs |  1.33 |    0.30 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.976 μs | 11.793 μs | 0.6464 μs |  1.02 |    0.19 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.963 μs |  6.252 μs | 0.3427 μs |  1.27 |    0.17 |    2 |      64 B |        1.14 |
