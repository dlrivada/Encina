```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.048 μs** | **2.7927 μs** | **0.1531 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.317 μs | 2.1765 μs | 0.1193 μs |  0.76 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.464 μs | 2.3926 μs | 0.1311 μs |  0.81 |    0.05 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.734 μs | 2.9323 μs | 0.1607 μs |  1.23 |    0.07 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.136 μs | 3.1557 μs | 0.1730 μs |  2.02 |    0.10 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.189 μs | 2.1302 μs | 0.1168 μs |  1.05 |    0.06 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.296 μs | 1.4956 μs | 0.0820 μs |  1.08 |    0.05 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.374 μs | 5.0284 μs | 0.2756 μs |  1.77 |    0.11 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.446 μs** | **2.5968 μs** | **0.1423 μs** |  **1.00** |    **0.05** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.217 μs | 2.3913 μs | 0.1311 μs |  0.64 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.591 μs | 0.2107 μs | 0.0115 μs |  0.75 |    0.03 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.753 μs | 4.6437 μs | 0.2545 μs |  1.09 |    0.07 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.657 μs | 6.9911 μs | 0.3832 μs |  2.22 |    0.12 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.349 μs | 4.1445 μs | 0.2272 μs |  0.97 |    0.07 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.364 μs | 1.4963 μs | 0.0820 μs |  1.27 |    0.05 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.184 μs | 3.8080 μs | 0.2087 μs |  1.80 |    0.08 |    3 |     152 B |        2.71 |
