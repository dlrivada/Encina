```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.005 μs** | **29.681 μs** | **1.6269 μs** | **1.257 μs** |  **1.46** |    **1.37** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 1.906 μs | 24.448 μs | 1.3401 μs | 1.653 μs |  1.39 |    1.19 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 2.362 μs | 31.219 μs | 1.7112 μs | 1.808 μs |  1.72 |    1.50 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.567 μs | 11.076 μs | 0.6071 μs | 2.384 μs |  1.87 |    1.07 |    3 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 2.834 μs |  3.187 μs | 0.1747 μs | 2.755 μs |  2.06 |    1.08 |    3 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **2.407 μs** | **31.816 μs** | **1.7439 μs** | **1.513 μs** |  **1.33** |    **1.09** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.064 μs | 27.893 μs | 1.5289 μs | 2.263 μs |  1.70 |    1.11 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 2.847 μs | 31.492 μs | 1.7262 μs | 2.183 μs |  1.58 |    1.15 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.168 μs |  4.235 μs | 0.2322 μs | 3.195 μs |  1.75 |    0.80 |    2 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 2.410 μs | 23.498 μs | 1.2880 μs | 1.833 μs |  1.33 |    0.90 |    1 |      64 B |        1.14 |
