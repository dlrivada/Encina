```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **4.725 μs** | **39.225 μs** | **2.1501 μs** | **5.019 μs** |  **1.19** |    **0.76** |    **3** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.658 μs | 23.225 μs | 1.2730 μs | 2.037 μs |  0.67 |    0.44 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.673 μs | 45.934 μs | 2.5178 μs | 5.469 μs |  1.18 |    0.82 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.203 μs | 27.592 μs | 1.5124 μs | 1.389 μs |  0.56 |    0.45 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.812 μs | 31.562 μs | 1.7300 μs | 4.599 μs |  0.96 |    0.62 |    3 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.614 μs** |  **2.526 μs** | **0.1384 μs** | **4.567 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.785 μs | 10.783 μs | 0.5911 μs | 3.716 μs |  0.82 |    0.11 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 6.747 μs | 15.805 μs | 0.8663 μs | 6.827 μs |  1.46 |    0.17 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.217 μs | 11.879 μs | 0.6511 μs | 3.085 μs |  0.70 |    0.12 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.390 μs |  9.871 μs | 0.5410 μs | 4.283 μs |  0.95 |    0.10 |    1 |      64 B |        1.14 |
