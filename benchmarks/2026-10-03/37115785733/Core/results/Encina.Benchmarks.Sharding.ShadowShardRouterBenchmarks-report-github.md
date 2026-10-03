```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error      | StdDev     | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|-----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **2.991 μs** |  **16.638 μs** |  **0.9120 μs** | **2.5840 μs** |  **1.06** |    **0.37** |    **3** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 1.474 μs |  21.853 μs |  1.1979 μs | 1.0370 μs |  0.52 |    0.39 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 2.178 μs |  27.883 μs |  1.5283 μs | 1.3775 μs |  0.77 |    0.51 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 1.469 μs |  19.568 μs |  1.0726 μs | 0.9320 μs |  0.52 |    0.36 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 2.885 μs |  17.053 μs |  0.9347 μs | 2.5240 μs |  1.02 |    0.37 |    3 |      64 B |        1.14 |
|                                          |            |          |            |            |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **1.895 μs** |  **18.412 μs** |  **1.0092 μs** | **1.3375 μs** |  **1.17** |    **0.70** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 2.267 μs |  21.394 μs |  1.1727 μs | 1.7720 μs |  1.40 |    0.83 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 9.003 μs | 225.981 μs | 12.3868 μs | 2.0020 μs |  5.56 |    7.25 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 2.865 μs |   7.824 μs |  0.4289 μs | 2.6485 μs |  1.77 |    0.67 |    2 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 2.080 μs |  20.950 μs |  1.1483 μs | 1.4430 μs |  1.28 |    0.79 |    1 |      64 B |        1.14 |
