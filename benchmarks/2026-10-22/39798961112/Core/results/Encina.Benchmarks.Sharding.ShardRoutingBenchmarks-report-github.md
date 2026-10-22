```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.075 μs** | **0.1315 μs** | **0.1969 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.036 μs | 0.0657 μs | 0.0942 μs |  0.66 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.489 μs | 0.0774 μs | 0.1110 μs |  0.81 |    0.06 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.699 μs | 0.0681 μs | 0.0976 μs |  1.21 |    0.08 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.249 μs | 0.5080 μs | 0.7286 μs |  2.04 |    0.26 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.916 μs | 0.0650 μs | 0.0890 μs |  0.95 |    0.06 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.500 μs | 0.1963 μs | 0.2620 μs |  1.14 |    0.11 |    4 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.647 μs | 0.3275 μs | 0.4591 μs |  1.84 |    0.18 |    5 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.555 μs** | **0.1652 μs** | **0.2316 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.147 μs | 0.0743 μs | 0.1042 μs |  0.61 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.607 μs | 0.1230 μs | 0.1764 μs |  0.74 |    0.07 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.679 μs | 0.7534 μs | 1.1276 μs |  1.32 |    0.32 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 8.075 μs | 0.5633 μs | 0.8078 μs |  2.28 |    0.26 |    6 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.355 μs | 0.1467 μs | 0.2150 μs |  0.95 |    0.08 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.728 μs | 0.0352 μs | 0.0470 μs |  1.05 |    0.06 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.405 μs | 0.0535 μs | 0.0750 μs |  1.53 |    0.09 |    5 |     152 B |        2.71 |
