```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.930 μs** | **0.1803 μs** | **0.2528 μs** |  **1.01** |    **0.12** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.996 μs | 0.1183 μs | 0.1734 μs |  0.69 |    0.08 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.709 μs | 0.1643 μs | 0.2408 μs |  0.93 |    0.11 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.613 μs | 0.1414 μs | 0.1983 μs |  1.24 |    0.12 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.484 μs | 0.1656 μs | 0.2375 μs |  1.54 |    0.15 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.885 μs | 0.1154 μs | 0.1617 μs |  0.99 |    0.10 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.346 μs | 0.1579 μs | 0.2315 μs |  1.15 |    0.12 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.288 μs | 0.1748 μs | 0.2450 μs |  1.82 |    0.17 |    5 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.465 μs** | **0.1248 μs** | **0.1790 μs** |  **1.00** |    **0.07** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.172 μs | 0.1878 μs | 0.2508 μs |  0.63 |    0.08 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.588 μs | 0.1026 μs | 0.1503 μs |  0.75 |    0.06 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.122 μs | 0.2315 μs | 0.3245 μs |  1.19 |    0.11 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 6.691 μs | 0.1891 μs | 0.2713 μs |  1.94 |    0.12 |    6 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.278 μs | 0.1176 μs | 0.1649 μs |  0.95 |    0.07 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.813 μs | 0.1777 μs | 0.2491 μs |  1.10 |    0.09 |    4 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.921 μs | 0.2951 μs | 0.4137 μs |  1.71 |    0.14 |    5 |     152 B |        2.71 |
