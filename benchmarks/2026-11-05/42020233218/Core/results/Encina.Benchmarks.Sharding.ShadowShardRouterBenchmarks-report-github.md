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
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.373 μs** | **30.865 μs** | **1.6918 μs** | **1.924 μs** |  **1.43** |    **1.32** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.979 μs | 39.880 μs | 2.1860 μs | 4.696 μs |  2.40 |    1.94 |    3 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.177 μs | 40.315 μs | 2.2098 μs | 1.935 μs |  1.91 |    1.75 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.942 μs | 33.589 μs | 1.8411 μs | 2.526 μs |  1.77 |    1.53 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 4.133 μs | 27.144 μs | 1.4879 μs | 3.916 μs |  2.49 |    1.73 |    3 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.902 μs** |  **9.378 μs** | **0.5140 μs** | **4.644 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 6.171 μs | 17.051 μs | 0.9346 μs | 6.278 μs |  1.27 |    0.20 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 6.579 μs | 19.162 μs | 1.0503 μs | 6.026 μs |  1.35 |    0.22 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.324 μs | 22.186 μs | 1.2161 μs | 4.824 μs |  0.89 |    0.23 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 6.332 μs | 12.211 μs | 0.6693 μs | 6.625 μs |  1.30 |    0.16 |    2 |      64 B |        1.14 |
