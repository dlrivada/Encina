```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.065 μs** | **17.528 μs** | **0.9608 μs** | **2.2185 μs** |  **1.21** |    **0.80** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 1.403 μs | 18.999 μs | 1.0414 μs | 1.0355 μs |  0.82 |    0.71 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 2.036 μs | 24.195 μs | 1.3262 μs | 1.6030 μs |  1.19 |    0.95 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 1.512 μs | 21.032 μs | 1.1528 μs | 1.1015 μs |  0.89 |    0.78 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 1.420 μs | 21.507 μs | 1.1789 μs | 0.9060 μs |  0.83 |    0.78 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **2.873 μs** | **20.113 μs** | **1.1025 μs** | **2.8190 μs** |  **1.11** |    **0.55** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 1.683 μs | 17.111 μs | 0.9379 μs | 1.1820 μs |  0.65 |    0.40 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 2.582 μs | 22.632 μs | 1.2405 μs | 1.9580 μs |  1.00 |    0.56 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 1.549 μs | 19.213 μs | 1.0531 μs | 0.9920 μs |  0.60 |    0.43 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.135 μs | 15.059 μs | 0.8255 μs | 2.7150 μs |  1.21 |    0.51 |    2 |      64 B |        1.14 |
