```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.836 μs** | **12.2876 μs** | **0.6735 μs** |  **1.02** |    **0.21** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 4.304 μs | 12.7299 μs | 0.6978 μs |  1.14 |    0.23 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 5.179 μs | 11.2536 μs | 0.6168 μs |  1.38 |    0.24 |    1 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 4.604 μs | 22.2385 μs | 1.2190 μs |  1.22 |    0.33 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 5.432 μs | 36.7397 μs | 2.0138 μs |  1.44 |    0.51 |    1 |      64 B |        1.14 |
|                                          |            |          |            |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **4.893 μs** |  **8.9419 μs** | **0.4901 μs** |  **1.01** |    **0.13** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 4.655 μs |  0.0380 μs | 0.0021 μs |  0.96 |    0.09 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 7.252 μs |  5.6337 μs | 0.3088 μs |  1.49 |    0.15 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.591 μs |  7.6812 μs | 0.4210 μs |  0.74 |    0.10 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 6.471 μs | 10.1264 μs | 0.5551 μs |  1.33 |    0.16 |    3 |      64 B |        1.14 |
