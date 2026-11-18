```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.679 μs** | **11.348 μs** | **0.6220 μs** |  **1.02** |    **0.20** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.199 μs |  2.470 μs | 0.1354 μs |  0.61 |    0.09 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 4.723 μs | 13.798 μs | 0.7563 μs |  1.31 |    0.26 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.399 μs | 17.780 μs | 0.9746 μs |  1.22 |    0.29 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 5.652 μs |  8.239 μs | 0.4516 μs |  1.56 |    0.24 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.540 μs |  9.979 μs | 0.5470 μs |  0.98 |    0.19 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.634 μs | 14.779 μs | 0.8101 μs |  1.01 |    0.24 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.636 μs | 13.338 μs | 0.7311 μs |  1.84 |    0.31 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **4.142 μs** |  **6.722 μs** | **0.3685 μs** |  **1.01** |    **0.11** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.478 μs |  5.724 μs | 0.3138 μs |  0.60 |    0.08 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.171 μs |  8.729 μs | 0.4785 μs |  0.77 |    0.12 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 7.899 μs | 13.841 μs | 0.7587 μs |  1.92 |    0.22 |    5 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.695 μs | 18.479 μs | 1.0129 μs |  1.87 |    0.26 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.139 μs |  8.688 μs | 0.4762 μs |  1.00 |    0.13 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 5.423 μs | 12.589 μs | 0.6901 μs |  1.32 |    0.18 |    4 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 7.116 μs | 21.116 μs | 1.1575 μs |  1.73 |    0.28 |    5 |     152 B |        2.71 |
