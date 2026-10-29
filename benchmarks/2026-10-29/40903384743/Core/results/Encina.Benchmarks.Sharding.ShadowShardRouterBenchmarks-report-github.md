```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.539 μs** |  **7.580 μs** | **0.4155 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.018 μs |  9.592 μs | 0.5258 μs |  0.86 |    0.16 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.183 μs |  7.054 μs | 0.3867 μs |  1.19 |    0.16 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.230 μs |  6.372 μs | 0.3493 μs |  0.92 |    0.13 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.955 μs |  9.391 μs | 0.5148 μs |  1.13 |    0.17 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.597 μs** |  **4.367 μs** | **0.2394 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 4.096 μs |  9.627 μs | 0.5277 μs |  0.89 |    0.11 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.779 μs | 11.187 μs | 0.6132 μs |  1.26 |    0.13 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 4.189 μs |  9.377 μs | 0.5140 μs |  0.91 |    0.11 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 5.169 μs |  9.256 μs | 0.5074 μs |  1.13 |    0.11 |    1 |      64 B |        1.14 |
