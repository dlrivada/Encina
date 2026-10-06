```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.320 μs** |  **4.369 μs** | **0.2395 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.175 μs |  3.872 μs | 0.2122 μs |  0.66 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.923 μs |  2.945 μs | 0.1614 μs |  0.88 |    0.07 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.181 μs | 10.597 μs | 0.5808 μs |  1.26 |    0.17 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.964 μs | 12.741 μs | 0.6984 μs |  1.50 |    0.21 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.166 μs |  2.887 μs | 0.1582 μs |  0.96 |    0.07 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.499 μs | 11.942 μs | 0.6546 μs |  1.06 |    0.18 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.815 μs | 12.970 μs | 0.7109 μs |  1.76 |    0.22 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.822 μs** | **11.886 μs** | **0.6515 μs** |  **1.02** |    **0.21** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.245 μs |  3.141 μs | 0.1721 μs |  0.60 |    0.09 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.273 μs |  2.844 μs | 0.1559 μs |  0.87 |    0.13 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.332 μs | 12.252 μs | 0.6716 μs |  1.15 |    0.22 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.185 μs |  8.781 μs | 0.4813 μs |  1.91 |    0.29 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.469 μs |  1.476 μs | 0.0809 μs |  0.92 |    0.13 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.995 μs |  7.779 μs | 0.4264 μs |  1.06 |    0.18 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.077 μs | 10.490 μs | 0.5750 μs |  1.62 |    0.26 |    3 |     152 B |        2.71 |
