```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.467 μs** | **10.025 μs** | **0.5495 μs** |  **1.03** |    **0.27** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.535 μs |  1.780 μs | 0.0976 μs |  0.64 |    0.12 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.037 μs |  5.185 μs | 0.2842 μs |  0.85 |    0.18 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.555 μs |  3.490 μs | 0.1913 μs |  1.49 |    0.27 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.198 μs | 12.336 μs | 0.6762 μs |  1.75 |    0.39 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.910 μs |  5.537 μs | 0.3035 μs |  1.22 |    0.24 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.172 μs | 12.226 μs | 0.6701 μs |  1.33 |    0.34 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.648 μs | 13.611 μs | 0.7461 μs |  1.94 |    0.43 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **2.814 μs** |  **2.659 μs** | **0.1458 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.856 μs |  2.001 μs | 0.1097 μs |  0.66 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.156 μs |  9.001 μs | 0.4934 μs |  1.12 |    0.16 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.489 μs | 11.994 μs | 0.6574 μs |  1.24 |    0.21 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.912 μs |  2.608 μs | 0.1429 μs |  2.10 |    0.10 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 2.978 μs |  6.302 μs | 0.3454 μs |  1.06 |    0.12 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.336 μs |  8.269 μs | 0.4533 μs |  1.19 |    0.15 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.928 μs | 10.494 μs | 0.5752 μs |  1.75 |    0.19 |    3 |     152 B |        2.71 |
