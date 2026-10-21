```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.368 μs** | **16.325 μs** | **0.8949 μs** | **2.829 μs** |  **1.14** |    **0.62** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.972 μs |  8.867 μs | 0.4860 μs | 2.975 μs |  1.43 |    0.63 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.911 μs | 24.213 μs | 1.3272 μs | 3.180 μs |  1.88 |    0.98 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.295 μs | 20.606 μs | 1.1295 μs | 2.715 μs |  1.58 |    0.83 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 1.646 μs | 21.750 μs | 1.1922 μs | 1.212 μs |  0.79 |    0.63 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **2.789 μs** | **23.732 μs** | **1.3008 μs** | **2.068 μs** |  **1.13** |    **0.60** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 1.984 μs | 21.561 μs | 1.1818 μs | 1.448 μs |  0.80 |    0.50 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 2.766 μs | 27.107 μs | 1.4858 μs | 2.288 μs |  1.12 |    0.65 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 2.550 μs | 30.338 μs | 1.6629 μs | 1.702 μs |  1.03 |    0.69 |    2 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 2.853 μs | 32.863 μs | 1.8013 μs | 1.817 μs |  1.15 |    0.76 |    2 |      64 B |        1.14 |
