```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **1.893 μs** | **27.770 μs** | **1.5222 μs** | **1.2220 μs** |  **1.46** |    **1.38** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.623 μs | 11.932 μs | 0.6540 μs | 2.3385 μs |  2.02 |    1.18 |    4 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 2.190 μs | 29.503 μs | 1.6171 μs | 1.5130 μs |  1.69 |    1.50 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.128 μs | 18.915 μs | 1.0368 μs | 2.7240 μs |  2.41 |    1.50 |    4 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 1.602 μs | 25.038 μs | 1.3724 μs | 0.8810 μs |  1.23 |    1.22 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **2.305 μs** | **19.247 μs** | **1.0550 μs** | **1.7780 μs** |  **1.13** |    **0.59** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.045 μs |  1.695 μs | 0.0929 μs | 3.0885 μs |  1.49 |    0.47 |    3 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 3.642 μs | 36.165 μs | 1.9823 μs | 3.1350 μs |  1.78 |    1.04 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 1.892 μs | 23.525 μs | 1.2895 μs | 1.3680 μs |  0.92 |    0.64 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 1.935 μs | 24.890 μs | 1.3643 μs | 1.1570 μs |  0.94 |    0.67 |    1 |      64 B |        1.14 |
