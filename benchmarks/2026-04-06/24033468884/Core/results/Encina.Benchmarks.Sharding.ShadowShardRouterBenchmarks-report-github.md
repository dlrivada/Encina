```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.625 μs** | **0.4264 μs** | **0.6250 μs** | **3.799 μs** |  **1.04** |    **0.29** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.692 μs | 0.4378 μs | 0.6279 μs | 2.648 μs |  0.77 |    0.25 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.321 μs | 0.6254 μs | 0.8970 μs | 4.385 μs |  1.24 |    0.37 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.923 μs | 0.1252 μs | 0.1795 μs | 2.926 μs |  0.84 |    0.19 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.336 μs | 0.3398 μs | 0.4981 μs | 3.193 μs |  0.96 |    0.25 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.284 μs** | **0.6387 μs** | **0.9160 μs** | **3.778 μs** |  **1.04** |    **0.29** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 4.705 μs | 0.7420 μs | 1.0401 μs | 5.134 μs |  1.14 |    0.33 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 4.937 μs | 0.2973 μs | 0.4264 μs | 4.822 μs |  1.20 |    0.24 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.412 μs | 0.2572 μs | 0.3605 μs | 3.275 μs |  0.83 |    0.17 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.192 μs | 0.1874 μs | 0.2627 μs | 4.125 μs |  1.02 |    0.19 |    2 |      64 B |        1.14 |
