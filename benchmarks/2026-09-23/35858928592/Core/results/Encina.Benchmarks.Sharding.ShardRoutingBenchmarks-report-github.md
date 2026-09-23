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
| **&#39;Hash routing&#39;**                   | **3**          | **3.229 μs** |  **1.6951 μs** | **0.0929 μs** |  **1.00** |    **0.03** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.037 μs |  2.0052 μs | 0.1099 μs |  0.63 |    0.03 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.608 μs |  2.9323 μs | 0.1607 μs |  0.81 |    0.05 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.987 μs |  4.3442 μs | 0.2381 μs |  1.24 |    0.07 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.074 μs |  3.1955 μs | 0.1752 μs |  1.88 |    0.07 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.180 μs |  0.9104 μs | 0.0499 μs |  0.99 |    0.03 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.459 μs |  3.8648 μs | 0.2118 μs |  1.07 |    0.06 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.159 μs |  6.4356 μs | 0.3528 μs |  1.91 |    0.11 |    3 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.520 μs** |  **4.8486 μs** | **0.2658 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.230 μs |  2.3170 μs | 0.1270 μs |  0.64 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.578 μs |  2.6501 μs | 0.1453 μs |  0.73 |    0.06 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.965 μs |  2.6310 μs | 0.1442 μs |  1.13 |    0.08 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.707 μs |  8.5291 μs | 0.4675 μs |  2.20 |    0.18 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.399 μs |  3.6948 μs | 0.2025 μs |  0.97 |    0.08 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.629 μs |  3.0571 μs | 0.1676 μs |  1.32 |    0.09 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.266 μs | 12.3212 μs | 0.6754 μs |  1.79 |    0.20 |    3 |     152 B |        2.71 |
