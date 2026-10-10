```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.792 μs** |  **9.5475 μs** | **0.5233 μs** |  **1.02** |    **0.23** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.579 μs |  4.4419 μs | 0.2435 μs |  0.58 |    0.12 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.070 μs |  3.5539 μs | 0.1948 μs |  0.76 |    0.13 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 2.914 μs |  9.3177 μs | 0.5107 μs |  1.07 |    0.23 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 3.749 μs |  7.4301 μs | 0.4073 μs |  1.37 |    0.25 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.554 μs |  1.8515 μs | 0.1015 μs |  0.94 |    0.15 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.651 μs |  4.3987 μs | 0.2411 μs |  0.97 |    0.17 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.761 μs |  8.3615 μs | 0.4583 μs |  1.74 |    0.31 |    5 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.223 μs** |  **3.9650 μs** | **0.2173 μs** |  **1.00** |    **0.08** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.754 μs |  3.1068 μs | 0.1703 μs |  0.55 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.169 μs |  0.5267 μs | 0.0289 μs |  0.68 |    0.04 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.518 μs |  8.0323 μs | 0.4403 μs |  1.09 |    0.13 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.625 μs | 15.8670 μs | 0.8697 μs |  1.75 |    0.25 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 2.787 μs |  8.4032 μs | 0.4606 μs |  0.87 |    0.13 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.395 μs | 14.1534 μs | 0.7758 μs |  1.06 |    0.22 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.664 μs |  5.8645 μs | 0.3215 μs |  1.45 |    0.12 |    4 |     152 B |        2.71 |
