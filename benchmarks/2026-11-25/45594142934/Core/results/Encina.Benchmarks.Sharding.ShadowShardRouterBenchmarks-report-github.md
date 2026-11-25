```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev   | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.630 μs** | **31.928 μs** | **1.750 μs** | **3.060 μs** |  **1.15** |    **0.66** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 4.358 μs | 46.897 μs | 2.571 μs | 3.404 μs |  1.38 |    0.90 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 5.545 μs | 18.390 μs | 1.008 μs | 5.889 μs |  1.76 |    0.71 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 4.788 μs | 46.693 μs | 2.559 μs | 4.659 μs |  1.52 |    0.93 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 5.283 μs | 39.440 μs | 2.162 μs | 5.180 μs |  1.68 |    0.88 |    1 |      64 B |        1.14 |
|                                          |            |          |           |          |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.620 μs** | **19.982 μs** | **1.095 μs** | **4.372 μs** |  **1.04** |    **0.30** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 5.244 μs | 18.277 μs | 1.002 μs | 5.157 μs |  1.18 |    0.30 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 7.613 μs | 26.307 μs | 1.442 μs | 7.106 μs |  1.71 |    0.44 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 5.779 μs | 31.711 μs | 1.738 μs | 5.098 μs |  1.30 |    0.43 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 5.679 μs | 25.280 μs | 1.386 μs | 5.224 μs |  1.27 |    0.37 |    1 |      64 B |        1.14 |
