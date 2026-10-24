```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean      | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          |  **5.335 μs** |  **56.816 μs** | **3.1143 μs** | **3.929 μs** |  **1.21** |    **0.81** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |  2.199 μs |   4.476 μs | 0.2453 μs | 2.170 μs |  0.50 |    0.20 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |  2.836 μs |   6.692 μs | 0.3668 μs | 2.755 μs |  0.64 |    0.27 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |  6.287 μs |  17.034 μs | 0.9337 μs | 6.415 μs |  1.43 |    0.60 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          |  5.341 μs |  15.334 μs | 0.8405 μs | 4.987 μs |  1.21 |    0.51 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |  3.479 μs |   8.278 μs | 0.4538 μs | 3.518 μs |  0.79 |    0.33 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |  3.560 μs |   8.421 μs | 0.4616 μs | 3.359 μs |  0.81 |    0.34 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 12.940 μs | 144.274 μs | 7.9082 μs | 9.486 μs |  2.94 |    2.03 |    5 |     152 B |        2.71 |
|                                  |            |           |            |           |          |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |  **4.530 μs** |   **9.290 μs** | **0.5092 μs** | **4.571 μs** |  **1.01** |    **0.14** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |  4.704 μs |   1.330 μs | 0.0729 μs | 4.667 μs |  1.05 |    0.10 |    2 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         |  3.233 μs |   8.004 μs | 0.4387 μs | 3.333 μs |  0.72 |    0.11 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         |  5.051 μs |  10.581 μs | 0.5800 μs | 4.864 μs |  1.12 |    0.16 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         |  7.539 μs |  15.490 μs | 0.8490 μs | 7.504 μs |  1.68 |    0.23 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |  4.497 μs |   9.710 μs | 0.5322 μs | 4.224 μs |  1.00 |    0.14 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         |  4.559 μs |   8.980 μs | 0.4922 μs | 4.469 μs |  1.02 |    0.14 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         |  6.756 μs |  14.095 μs | 0.7726 μs | 6.518 μs |  1.50 |    0.21 |    3 |     152 B |        2.71 |
