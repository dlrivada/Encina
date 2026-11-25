```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **4.067 μs** | **25.174 μs** | **1.3799 μs** | **4.160 μs** |  **1.09** |    **0.48** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.333 μs | 24.000 μs | 1.3155 μs | 2.582 μs |  0.89 |    0.43 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 5.154 μs | 37.179 μs | 2.0379 μs | 4.569 μs |  1.38 |    0.66 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.749 μs | 26.908 μs | 1.4749 μs | 3.071 μs |  1.00 |    0.48 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 4.275 μs | 24.023 μs | 1.3168 μs | 3.897 μs |  1.15 |    0.49 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.602 μs** | **18.274 μs** | **1.0016 μs** | **4.392 μs** |  **1.03** |    **0.27** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 6.696 μs | 10.041 μs | 0.5504 μs | 6.412 μs |  1.50 |    0.29 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 8.559 μs | 18.569 μs | 1.0178 μs | 8.101 μs |  1.92 |    0.40 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 6.920 μs | 22.091 μs | 1.2109 μs | 6.755 μs |  1.55 |    0.37 |    2 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 5.819 μs | 17.718 μs | 0.9712 μs | 5.902 μs |  1.30 |    0.30 |    2 |      64 B |        1.14 |
