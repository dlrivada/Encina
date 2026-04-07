```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.990 μs** | **0.1937 μs** | **0.2715 μs** | **2.985 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.107 μs | 0.0901 μs | 0.1263 μs | 3.086 μs |  1.05 |    0.10 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.501 μs | 0.4185 μs | 0.6002 μs | 4.834 μs |  1.52 |    0.24 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.208 μs | 0.1021 μs | 0.1431 μs | 3.226 μs |  1.08 |    0.11 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.568 μs | 0.1948 μs | 0.2855 μs | 3.467 μs |  1.20 |    0.14 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.713 μs** | **0.2548 μs** | **0.3655 μs** | **3.678 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.445 μs | 0.1251 μs | 0.1754 μs | 3.437 μs |  0.94 |    0.10 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 4.633 μs | 0.1634 μs | 0.2291 μs | 4.678 μs |  1.26 |    0.13 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.804 μs | 0.3740 μs | 0.5364 μs | 3.761 μs |  1.03 |    0.17 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.733 μs | 0.0590 μs | 0.0828 μs | 3.726 μs |  1.01 |    0.10 |    1 |      64 B |        1.14 |
