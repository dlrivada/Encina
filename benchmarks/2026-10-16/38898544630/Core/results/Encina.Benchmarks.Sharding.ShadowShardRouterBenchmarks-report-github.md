```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev   | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.418 μs** | **28.010 μs** | **1.535 μs** | **2.620 μs** |  **1.12** |    **0.57** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.325 μs | 21.259 μs | 1.165 μs | 2.776 μs |  1.09 |    0.48 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 6.255 μs | 36.692 μs | 2.011 μs | 5.233 μs |  2.05 |    0.87 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 5.655 μs | 38.369 μs | 2.103 μs | 4.635 μs |  1.85 |    0.85 |    2 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.332 μs | 35.007 μs | 1.919 μs | 2.507 μs |  1.09 |    0.66 |    1 |      64 B |        1.14 |
|                                          |            |          |           |          |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.256 μs** | **18.474 μs** | **1.013 μs** | **3.913 μs** |  **1.04** |    **0.29** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 5.062 μs | 22.615 μs | 1.240 μs | 4.557 μs |  1.23 |    0.35 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 7.199 μs | 32.382 μs | 1.775 μs | 6.486 μs |  1.75 |    0.50 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.386 μs | 24.549 μs | 1.346 μs | 3.981 μs |  1.07 |    0.35 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 5.833 μs | 27.225 μs | 1.492 μs | 5.194 μs |  1.42 |    0.42 |    1 |      64 B |        1.14 |
