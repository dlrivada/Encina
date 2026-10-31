```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.947 μs** | **11.453 μs** | **0.6278 μs** |  **1.02** |    **0.19** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.136 μs |  6.747 μs | 0.3698 μs |  0.55 |    0.11 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 4.280 μs | 25.418 μs | 1.3933 μs |  1.10 |    0.34 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.573 μs | 13.815 μs | 0.7572 μs |  1.18 |    0.23 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 5.715 μs |  6.887 μs | 0.3775 μs |  1.47 |    0.21 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.838 μs |  7.355 μs | 0.4032 μs |  0.99 |    0.16 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.671 μs |  8.494 μs | 0.4656 μs |  0.95 |    0.16 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.702 μs | 14.184 μs | 0.7775 μs |  1.73 |    0.28 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **4.131 μs** |  **2.409 μs** | **0.1321 μs** |  **1.00** |    **0.04** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 4.907 μs | 11.818 μs | 0.6478 μs |  1.19 |    0.14 |    3 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.512 μs |  7.878 μs | 0.4318 μs |  0.85 |    0.09 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 5.000 μs | 18.355 μs | 1.0061 μs |  1.21 |    0.21 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 8.537 μs | 13.239 μs | 0.7257 μs |  2.07 |    0.16 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 5.178 μs | 13.315 μs | 0.7298 μs |  1.25 |    0.16 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 5.018 μs | 12.402 μs | 0.6798 μs |  1.22 |    0.15 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.784 μs | 11.278 μs | 0.6182 μs |  1.64 |    0.14 |    4 |     152 B |        2.71 |
