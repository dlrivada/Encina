```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.172 μs** |  **1.1729 μs** | **0.0643 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.780 μs |  1.4746 μs | 0.0808 μs |  1.19 |    0.03 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 5.991 μs | 37.3984 μs | 2.0499 μs |  1.89 |    0.56 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.165 μs |  3.8416 μs | 0.2106 μs |  1.00 |    0.06 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 4.105 μs |  4.4185 μs | 0.2422 μs |  1.29 |    0.07 |    1 |      64 B |        1.14 |
|                                          |            |          |            |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.512 μs** |  **0.1824 μs** | **0.0100 μs** |  **1.00** |    **0.00** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.516 μs |  2.0665 μs | 0.1133 μs |  1.00 |    0.03 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 4.789 μs |  3.6729 μs | 0.2013 μs |  1.36 |    0.05 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.476 μs |  2.4024 μs | 0.1317 μs |  0.99 |    0.03 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 3.903 μs |  0.7456 μs | 0.0409 μs |  1.11 |    0.01 |    1 |      64 B |        1.14 |
