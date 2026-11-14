```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev   | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **1.614 μs** | **22.895 μs** | **1.255 μs** | **1.067 μs** |  **1.42** |    **1.30** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.483 μs | 22.634 μs | 1.241 μs | 1.802 μs |  2.19 |    1.55 |    3 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 2.699 μs | 39.983 μs | 2.192 μs | 2.009 μs |  2.38 |    2.23 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.187 μs | 26.696 μs | 1.463 μs | 1.372 μs |  1.93 |    1.59 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 2.000 μs | 30.205 μs | 1.656 μs | 1.542 μs |  1.76 |    1.68 |    2 |      64 B |        1.14 |
|                                          |            |          |           |          |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **2.320 μs** | **26.670 μs** | **1.462 μs** | **1.823 μs** |  **1.28** |    **0.97** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.893 μs | 22.447 μs | 1.230 μs | 3.296 μs |  2.15 |    1.20 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 3.880 μs | 36.461 μs | 1.999 μs | 3.089 μs |  2.14 |    1.45 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 2.673 μs | 25.612 μs | 1.404 μs | 1.928 μs |  1.47 |    1.01 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.205 μs | 26.180 μs | 1.435 μs | 2.634 μs |  1.77 |    1.12 |    2 |      64 B |        1.14 |
