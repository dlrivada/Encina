```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **1.571 μs** | **16.729 μs** | **0.9170 μs** | **1.377 μs** |  **1.27** |    **0.95** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.120 μs | 21.028 μs | 1.1526 μs | 2.373 μs |  1.71 |    1.23 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 2.682 μs | 34.977 μs | 1.9172 μs | 1.928 μs |  2.16 |    1.83 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 1.777 μs | 19.698 μs | 1.0797 μs | 1.246 μs |  1.43 |    1.10 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.191 μs | 24.144 μs | 1.3234 μs | 2.504 μs |  2.58 |    1.64 |    4 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.353 μs** | **13.273 μs** | **0.7276 μs** | **2.990 μs** |  **1.03** |    **0.26** |    **4** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 2.107 μs | 21.078 μs | 1.1553 μs | 1.552 μs |  0.65 |    0.33 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.117 μs | 24.592 μs | 1.3480 μs | 5.258 μs |  1.57 |    0.45 |    5 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 1.980 μs | 29.435 μs | 1.6134 μs | 1.192 μs |  0.61 |    0.45 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 2.591 μs | 27.938 μs | 1.5314 μs | 1.893 μs |  0.80 |    0.43 |    3 |      64 B |        1.14 |
