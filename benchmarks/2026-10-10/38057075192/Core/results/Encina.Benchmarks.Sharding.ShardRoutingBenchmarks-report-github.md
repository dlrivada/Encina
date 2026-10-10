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
| **&#39;Hash routing&#39;**                   | **3**          | **4.031 μs** | **10.342 μs** | **0.5669 μs** |  **1.01** |    **0.18** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.163 μs |  1.139 μs | 0.0624 μs |  0.54 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.621 μs |  7.063 μs | 0.3871 μs |  0.66 |    0.12 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.807 μs |  6.185 μs | 0.3390 μs |  0.96 |    0.14 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.389 μs |  1.391 μs | 0.0763 μs |  1.61 |    0.20 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.135 μs |  4.313 μs | 0.2364 μs |  0.79 |    0.11 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.481 μs |  4.637 μs | 0.2541 μs |  0.88 |    0.12 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.320 μs |  6.826 μs | 0.3742 μs |  1.34 |    0.19 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **4.762 μs** |  **1.148 μs** | **0.0629 μs** |  **1.00** |    **0.02** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.275 μs |  1.099 μs | 0.0603 μs |  0.48 |    0.01 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.699 μs |  2.189 μs | 0.1200 μs |  0.57 |    0.02 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.031 μs |  2.556 μs | 0.1401 μs |  0.85 |    0.03 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.786 μs | 11.940 μs | 0.6545 μs |  1.64 |    0.12 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.429 μs |  5.301 μs | 0.2905 μs |  0.72 |    0.05 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 5.998 μs | 30.865 μs | 1.6918 μs |  1.26 |    0.31 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.725 μs |  4.261 μs | 0.2336 μs |  1.20 |    0.04 |    3 |     152 B |        2.71 |
