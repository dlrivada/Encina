```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.233 μs** | **35.069 μs** | **1.9223 μs** | **1.275 μs** |  **1.51** |    **1.49** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.385 μs | 32.428 μs | 1.7775 μs | 1.397 μs |  1.62 |    1.44 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.338 μs | 39.423 μs | 2.1609 μs | 2.304 μs |  2.26 |    1.84 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 5.285 μs | 19.478 μs | 1.0677 μs | 5.492 μs |  3.58 |    2.00 |    4 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.857 μs | 22.330 μs | 1.2240 μs | 3.526 μs |  2.61 |    1.58 |    3 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.665 μs** |  **6.763 μs** | **0.3707 μs** | **4.755 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 6.593 μs | 37.147 μs | 2.0361 μs | 6.403 μs |  1.42 |    0.39 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.911 μs |  7.889 μs | 0.4324 μs | 5.804 μs |  1.27 |    0.12 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.728 μs | 28.368 μs | 1.5550 μs | 5.042 μs |  1.02 |    0.30 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 5.878 μs | 10.857 μs | 0.5951 μs | 5.604 μs |  1.27 |    0.14 |    1 |      64 B |        1.14 |
