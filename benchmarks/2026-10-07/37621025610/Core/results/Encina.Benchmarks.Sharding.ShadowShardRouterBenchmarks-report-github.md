```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.521 μs** | **26.751 μs** | **1.4663 μs** | **2.695 μs** |  **1.10** |    **0.52** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.313 μs | 31.546 μs | 1.7292 μs | 2.686 μs |  1.04 |    0.57 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.785 μs | 39.710 μs | 2.1767 μs | 4.230 μs |  1.50 |    0.75 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 4.085 μs | 32.568 μs | 1.7852 μs | 3.368 μs |  1.28 |    0.62 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.558 μs | 30.067 μs | 1.6481 μs | 2.835 μs |  1.11 |    0.57 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **6.537 μs** | **25.477 μs** | **1.3965 μs** | **6.127 μs** |  **1.03** |    **0.26** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 6.644 μs | 15.189 μs | 0.8326 μs | 6.186 μs |  1.05 |    0.21 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.870 μs | 28.841 μs | 1.5808 μs | 4.971 μs |  0.92 |    0.27 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.972 μs | 21.243 μs | 1.1644 μs | 4.319 μs |  0.78 |    0.21 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 5.193 μs | 21.910 μs | 1.2010 μs | 4.957 μs |  0.82 |    0.22 |    1 |      64 B |        1.14 |
