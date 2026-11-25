```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.324 μs** |  **5.701 μs** | **0.3125 μs** |  **1.01** |    **0.16** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.412 μs |  4.218 μs | 0.2312 μs |  0.61 |    0.11 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.103 μs |  3.956 μs | 0.2168 μs |  0.91 |    0.13 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.121 μs |  3.109 μs | 0.1704 μs |  1.36 |    0.16 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.591 μs |  6.770 μs | 0.3711 μs |  2.00 |    0.26 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.527 μs |  7.700 μs | 0.4220 μs |  1.10 |    0.20 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.700 μs |  5.139 μs | 0.2817 μs |  1.17 |    0.17 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.827 μs |  4.593 μs | 0.2518 μs |  2.10 |    0.25 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.289 μs** |  **8.374 μs** | **0.4590 μs** |  **1.01** |    **0.17** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.606 μs |  2.507 μs | 0.1374 μs |  0.49 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.475 μs |  2.671 μs | 0.1464 μs |  0.76 |    0.09 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.922 μs | 11.304 μs | 0.6196 μs |  1.21 |    0.21 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 6.600 μs | 14.669 μs | 0.8040 μs |  2.03 |    0.31 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.402 μs | 10.332 μs | 0.5663 μs |  1.05 |    0.19 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.205 μs |  4.495 μs | 0.2464 μs |  0.99 |    0.13 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.560 μs |  7.083 μs | 0.3882 μs |  1.40 |    0.19 |    4 |     152 B |        2.71 |
