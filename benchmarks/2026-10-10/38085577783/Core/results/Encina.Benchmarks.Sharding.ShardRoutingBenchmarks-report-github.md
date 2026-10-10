```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.579 μs** | **4.605 μs** | **0.2524 μs** |  **1.01** |    **0.12** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.449 μs | 3.103 μs | 0.1701 μs |  0.57 |    0.08 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 1.996 μs | 3.212 μs | 0.1761 μs |  0.78 |    0.09 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.035 μs | 6.599 μs | 0.3617 μs |  1.18 |    0.16 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 3.741 μs | 7.770 μs | 0.4259 μs |  1.46 |    0.19 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.697 μs | 4.738 μs | 0.2597 μs |  1.05 |    0.13 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.794 μs | 5.858 μs | 0.3211 μs |  1.09 |    0.14 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.434 μs | 5.912 μs | 0.3240 μs |  1.73 |    0.19 |    5 |     152 B |        2.71 |
|                                  |            |          |          |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.185 μs** | **5.227 μs** | **0.2865 μs** |  **1.01** |    **0.11** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.886 μs | 4.893 μs | 0.2682 μs |  0.60 |    0.09 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.264 μs | 2.594 μs | 0.1422 μs |  0.71 |    0.07 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.238 μs | 3.567 μs | 0.1955 μs |  1.02 |    0.09 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.474 μs | 3.801 μs | 0.2084 μs |  1.73 |    0.14 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 2.879 μs | 7.912 μs | 0.4337 μs |  0.91 |    0.14 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.529 μs | 3.592 μs | 0.1969 μs |  1.11 |    0.10 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.544 μs | 5.184 μs | 0.2842 μs |  1.43 |    0.13 |    4 |     152 B |        2.71 |
