```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                   | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |------------:|------:|------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **18,646.7 μs** |    **NA** |  **1.00** |    **4** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 17,566.2 μs |    NA |  0.94 |    3 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 16,477.6 μs |    NA |  0.88 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          |    632.0 μs |    NA |  0.03 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 18,997.8 μs |    NA |  1.02 |    5 |      64 B |        1.14 |
|                                          |            |             |       |       |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **17,619.3 μs** |    **NA** |  **1.00** |    **3** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 17,635.5 μs |    NA |  1.00 |    4 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 16,389.0 μs |    NA |  0.93 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         |    697.2 μs |    NA |  0.04 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 18,027.1 μs |    NA |  1.02 |    5 |      64 B |        1.14 |
