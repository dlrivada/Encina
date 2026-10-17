```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.169 μs** |  **0.8227 μs** | **0.0451 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.148 μs |  2.6686 μs | 0.1463 μs |  0.99 |    0.04 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.819 μs | 12.5082 μs | 0.6856 μs |  1.52 |    0.19 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.506 μs | 10.0118 μs | 0.5488 μs |  1.11 |    0.15 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.969 μs |  3.0546 μs | 0.1674 μs |  1.25 |    0.05 |    1 |      64 B |        1.14 |
|                                          |            |          |            |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.058 μs** |  **7.7739 μs** | **0.4261 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 4.259 μs | 11.6189 μs | 0.6369 μs |  1.06 |    0.16 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.942 μs |  8.7433 μs | 0.4793 μs |  1.47 |    0.16 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.278 μs | 12.7564 μs | 0.6992 μs |  1.06 |    0.18 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.085 μs |  5.7026 μs | 0.3126 μs |  1.01 |    0.11 |    1 |      64 B |        1.14 |
