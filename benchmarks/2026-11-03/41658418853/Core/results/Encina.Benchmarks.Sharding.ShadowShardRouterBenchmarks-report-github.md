```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.621 μs** | **30.337 μs** | **1.6629 μs** | **1.995 μs** |  **1.27** |    **0.96** |    **3** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.543 μs | 31.822 μs | 1.7442 μs | 3.345 μs |  1.24 |    0.98 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.804 μs | 29.340 μs | 1.6082 μs | 3.004 μs |  1.85 |    1.12 |    4 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.428 μs | 29.985 μs | 1.6436 μs | 1.963 μs |  1.18 |    0.93 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 2.349 μs | 28.972 μs | 1.5881 μs | 2.066 μs |  1.14 |    0.90 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.912 μs** |  **8.447 μs** | **0.4630 μs** | **3.712 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.542 μs |  8.825 μs | 0.4837 μs | 3.635 μs |  0.91 |    0.14 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 7.772 μs | 23.913 μs | 1.3107 μs | 8.325 μs |  2.00 |    0.35 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.586 μs | 14.593 μs | 0.7999 μs | 3.235 μs |  0.92 |    0.20 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.393 μs | 19.371 μs | 1.0618 μs | 4.231 μs |  1.13 |    0.26 |    1 |      64 B |        1.14 |
