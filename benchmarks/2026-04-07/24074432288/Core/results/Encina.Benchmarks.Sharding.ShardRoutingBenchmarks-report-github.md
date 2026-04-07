```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.064 μs** | **0.2210 μs** | **0.3169 μs** |  **1.01** |    **0.16** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.867 μs | 0.1021 μs | 0.1465 μs |  0.62 |    0.09 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.583 μs | 0.0954 μs | 0.1337 μs |  0.85 |    0.11 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.388 μs | 0.4455 μs | 0.6245 μs |  1.12 |    0.24 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 5.474 μs | 0.2510 μs | 0.3519 μs |  1.81 |    0.24 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.944 μs | 0.1624 μs | 0.2328 μs |  0.97 |    0.14 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.316 μs | 0.2971 μs | 0.4260 μs |  1.09 |    0.19 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.070 μs | 0.2860 μs | 0.3915 μs |  1.67 |    0.23 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.771 μs** | **0.1763 μs** | **0.2584 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.372 μs | 0.0640 μs | 0.0919 μs |  0.63 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.950 μs | 0.1356 μs | 0.1945 μs |  0.79 |    0.07 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.247 μs | 0.3662 μs | 0.5253 μs |  1.13 |    0.16 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 8.291 μs | 0.3021 μs | 0.4235 μs |  2.21 |    0.18 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.177 μs | 0.3832 μs | 0.5616 μs |  1.11 |    0.16 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.440 μs | 0.4803 μs | 0.6733 μs |  1.18 |    0.19 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.982 μs | 0.1878 μs | 0.2693 μs |  1.59 |    0.12 |    4 |     152 B |        2.71 |
