```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.191 μs** |  **0.3055 μs** | **0.0167 μs** |  **1.00** |    **0.01** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.722 μs |  2.5652 μs | 0.1406 μs |  0.85 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 3.704 μs | 12.5392 μs | 0.6873 μs |  1.16 |    0.19 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.840 μs |  2.8705 μs | 0.1573 μs |  1.20 |    0.04 |    1 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.349 μs |  2.6242 μs | 0.1438 μs |  1.99 |    0.04 |    2 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 4.175 μs |  4.1090 μs | 0.2252 μs |  1.31 |    0.06 |    1 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.628 μs |  4.4352 μs | 0.2431 μs |  1.14 |    0.07 |    1 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.972 μs |  9.1812 μs | 0.5033 μs |  1.87 |    0.14 |    2 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **4.598 μs** |  **7.9934 μs** | **0.4381 μs** |  **1.01** |    **0.12** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.869 μs |  1.0048 μs | 0.0551 μs |  0.63 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.034 μs |  5.8152 μs | 0.3187 μs |  0.66 |    0.08 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 5.249 μs | 15.3308 μs | 0.8403 μs |  1.15 |    0.19 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 9.601 μs | 13.9701 μs | 0.7657 μs |  2.10 |    0.22 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.295 μs |  7.9471 μs | 0.4356 μs |  0.94 |    0.11 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 6.103 μs | 16.3213 μs | 0.8946 μs |  1.34 |    0.20 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.741 μs |  6.1617 μs | 0.3377 μs |  1.26 |    0.12 |    2 |     152 B |        2.71 |
