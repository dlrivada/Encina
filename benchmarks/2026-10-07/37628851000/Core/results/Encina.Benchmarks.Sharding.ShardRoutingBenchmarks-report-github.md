```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.50GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.293 μs** | **11.138 μs** | **0.6105 μs** |  **1.02** |    **0.23** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.163 μs |  3.651 μs | 0.2002 μs |  0.67 |    0.12 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 5.327 μs | 15.124 μs | 0.8290 μs |  1.65 |    0.34 |    3 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.149 μs | 15.006 μs | 0.8225 μs |  1.29 |    0.30 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 5.753 μs |  9.096 μs | 0.4986 μs |  1.79 |    0.30 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 6.082 μs | 10.835 μs | 0.5939 μs |  1.89 |    0.33 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 4.507 μs | 12.843 μs | 0.7040 μs |  1.40 |    0.29 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.609 μs | 11.210 μs | 0.6144 μs |  2.05 |    0.35 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.871 μs** |  **4.241 μs** | **0.2325 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.308 μs |  3.769 μs | 0.2066 μs |  0.60 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 5.941 μs | 13.479 μs | 0.7388 μs |  1.54 |    0.18 |    3 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.590 μs |  8.200 μs | 0.4495 μs |  1.19 |    0.12 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.657 μs | 20.831 μs | 1.1418 μs |  1.98 |    0.28 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.245 μs | 15.629 μs | 0.8567 μs |  1.10 |    0.20 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.690 μs |  8.753 μs | 0.4798 μs |  1.21 |    0.12 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.619 μs | 14.413 μs | 0.7901 μs |  1.71 |    0.20 |    3 |     152 B |        2.71 |
