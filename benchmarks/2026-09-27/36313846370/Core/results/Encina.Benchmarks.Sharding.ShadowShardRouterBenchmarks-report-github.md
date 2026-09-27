```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **4.104 μs** |  **27.689 μs** | **1.5177 μs** | **3.414 μs** |  **1.08** |    **0.46** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 4.926 μs |  36.166 μs | 1.9824 μs | 4.391 μs |  1.30 |    0.58 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.173 μs |  39.399 μs | 2.1596 μs | 3.256 μs |  1.10 |    0.59 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.910 μs |   9.940 μs | 0.5448 μs | 3.617 μs |  1.03 |    0.31 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 8.268 μs | 152.122 μs | 8.3383 μs | 5.088 μs |  2.18 |    2.05 |    1 |      64 B |        1.14 |
|                                          |            |          |            |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.837 μs** |  **16.604 μs** | **0.9101 μs** | **3.490 μs** |  **1.03** |    **0.29** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 4.030 μs |  15.779 μs | 0.8649 μs | 3.936 μs |  1.09 |    0.29 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.143 μs |  28.417 μs | 1.5576 μs | 4.434 μs |  1.39 |    0.45 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.048 μs |  14.692 μs | 0.8053 μs | 3.843 μs |  1.09 |    0.28 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.717 μs |  18.953 μs | 1.0389 μs | 4.540 μs |  1.27 |    0.34 |    1 |      64 B |        1.14 |
