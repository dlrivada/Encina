```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **4.828 μs** | **27.447 μs** | **1.5045 μs** |  **1.06** |    **0.39** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 4.613 μs | 23.541 μs | 1.2903 μs |  1.01 |    0.35 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 6.298 μs | 27.948 μs | 1.5319 μs |  1.38 |    0.44 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 4.792 μs |  6.146 μs | 0.3369 μs |  1.05 |    0.26 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 5.432 μs | 14.718 μs | 0.8068 μs |  1.19 |    0.32 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **6.432 μs** |  **3.345 μs** | **0.1834 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 5.708 μs |  4.854 μs | 0.2661 μs |  0.89 |    0.04 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 9.155 μs | 38.588 μs | 2.1151 μs |  1.42 |    0.29 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 6.579 μs | 20.628 μs | 1.1307 μs |  1.02 |    0.15 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 7.692 μs | 11.406 μs | 0.6252 μs |  1.20 |    0.09 |    2 |      64 B |        1.14 |
