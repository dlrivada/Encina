```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.210 μs** |  **28.532 μs** | **1.5639 μs** | **2.512 μs** |  **1.15** |    **0.64** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 4.120 μs |  31.446 μs | 1.7237 μs | 3.430 μs |  1.47 |    0.75 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.194 μs |  33.256 μs | 1.8229 μs | 3.595 μs |  1.50 |    0.78 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.988 μs |  31.356 μs | 1.7187 μs | 2.182 μs |  1.07 |    0.67 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.104 μs |  31.885 μs | 1.7477 μs | 2.433 μs |  1.11 |    0.68 |    1 |      64 B |        1.14 |
|                                          |            |          |            |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.046 μs** |  **17.488 μs** | **0.9586 μs** | **3.671 μs** |  **1.03** |    **0.29** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.651 μs |  17.413 μs | 0.9545 μs | 3.143 μs |  0.93 |    0.28 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.350 μs |  24.572 μs | 1.3469 μs | 4.926 μs |  1.37 |    0.39 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 7.466 μs | 103.037 μs | 5.6478 μs | 5.273 μs |  1.91 |    1.32 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.827 μs |  23.308 μs | 1.2776 μs | 4.255 μs |  1.23 |    0.37 |    1 |      64 B |        1.14 |
