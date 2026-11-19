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
| **&#39;Hash routing&#39;**                   | **3**          | **3.495 μs** |  **5.771 μs** | **0.3163 μs** |  **1.01** |    **0.11** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.700 μs |  3.931 μs | 0.2155 μs |  0.49 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.113 μs |  2.659 μs | 0.1457 μs |  0.61 |    0.06 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.219 μs |  5.326 μs | 0.2920 μs |  0.93 |    0.11 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.117 μs |  8.078 μs | 0.4428 μs |  1.18 |    0.15 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.581 μs |  6.436 μs | 0.3528 μs |  0.74 |    0.11 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.398 μs |  8.319 μs | 0.4560 μs |  0.69 |    0.13 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.221 μs |  8.089 μs | 0.4434 μs |  1.21 |    0.15 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.068 μs** | **14.212 μs** | **0.7790 μs** |  **1.04** |    **0.31** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.732 μs |  2.032 μs | 0.1114 μs |  0.59 |    0.12 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.137 μs |  7.642 μs | 0.4189 μs |  1.06 |    0.24 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.655 μs |  4.867 μs | 0.2668 μs |  1.24 |    0.25 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.699 μs |  5.401 μs | 0.2960 μs |  1.93 |    0.38 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.087 μs |  5.501 μs | 0.3015 μs |  1.05 |    0.22 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.554 μs |  2.320 μs | 0.1272 μs |  1.20 |    0.23 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.858 μs | 11.759 μs | 0.6446 μs |  1.65 |    0.37 |    3 |     152 B |        2.71 |
