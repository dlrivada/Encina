```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.192 μs** | **1.2147 μs** | **0.0666 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.046 μs | 2.0068 μs | 0.1100 μs |  0.95 |    0.03 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.279 μs | 3.3362 μs | 0.1829 μs |  1.34 |    0.06 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.216 μs | 1.6820 μs | 0.0922 μs |  1.01 |    0.03 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.343 μs | 2.4866 μs | 0.1363 μs |  1.05 |    0.04 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.420 μs** | **0.5594 μs** | **0.0307 μs** |  **1.00** |    **0.01** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.547 μs | 4.7007 μs | 0.2577 μs |  1.04 |    0.07 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.772 μs | 2.5908 μs | 0.1420 μs |  1.69 |    0.04 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.273 μs | 2.4279 μs | 0.1331 μs |  0.96 |    0.03 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.631 μs | 1.7999 μs | 0.0987 μs |  1.35 |    0.03 |    2 |      64 B |        1.14 |
