```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          |  **3.006 μs** |   **2.468 μs** |  **0.1353 μs** | **2.996 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          |  3.062 μs |   3.660 μs |  0.2006 μs | 3.055 μs |  1.02 |    0.07 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          |  4.100 μs |   8.145 μs |  0.4465 μs | 4.077 μs |  1.37 |    0.14 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          |  3.273 μs |   7.369 μs |  0.4039 μs | 3.135 μs |  1.09 |    0.12 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          |  3.283 μs |   3.550 μs |  0.1946 μs | 3.186 μs |  1.09 |    0.07 |    1 |      64 B |        1.14 |
|                                          |            |           |            |            |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         |  **3.440 μs** |   **3.876 μs** |  **0.2124 μs** | **3.407 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         |  3.521 μs |   5.551 μs |  0.3043 μs | 3.437 μs |  1.03 |    0.09 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         |  5.677 μs |   6.604 μs |  0.3620 μs | 5.872 μs |  1.65 |    0.13 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         |  3.476 μs |   2.032 μs |  0.1114 μs | 3.456 μs |  1.01 |    0.06 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 20.178 μs | 514.128 μs | 28.1811 μs | 3.958 μs |  5.88 |    7.13 |    3 |      64 B |        1.14 |
