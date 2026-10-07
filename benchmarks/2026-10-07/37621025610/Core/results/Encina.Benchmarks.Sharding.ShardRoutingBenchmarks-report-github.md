```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.468 μs** |  **4.321 μs** | **0.2369 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.154 μs |  1.800 μs | 0.0986 μs |  0.62 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.549 μs |  2.151 μs | 0.1179 μs |  0.74 |    0.05 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.913 μs |  8.304 μs | 0.4551 μs |  1.13 |    0.13 |    1 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 5.962 μs |  5.536 μs | 0.3034 μs |  1.72 |    0.13 |    2 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.003 μs |  2.898 μs | 0.1589 μs |  0.87 |    0.07 |    1 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.430 μs |  2.026 μs | 0.1111 μs |  0.99 |    0.07 |    1 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.413 μs |  7.396 μs | 0.4054 μs |  1.57 |    0.14 |    2 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.556 μs** |  **3.607 μs** | **0.1977 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.247 μs |  2.123 μs | 0.1164 μs |  0.63 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.500 μs |  3.340 μs | 0.1831 μs |  0.70 |    0.06 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.870 μs |  3.213 μs | 0.1761 μs |  1.09 |    0.07 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 8.459 μs | 17.993 μs | 0.9863 μs |  2.38 |    0.27 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.328 μs | 11.047 μs | 0.6055 μs |  1.22 |    0.16 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.228 μs |  4.977 μs | 0.2728 μs |  1.19 |    0.09 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.535 μs |  2.563 μs | 0.1405 μs |  1.84 |    0.10 |    3 |     152 B |        2.71 |
