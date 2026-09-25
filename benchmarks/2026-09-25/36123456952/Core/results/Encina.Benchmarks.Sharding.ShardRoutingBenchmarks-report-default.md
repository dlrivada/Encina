
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error       | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|------------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.120 μs** |   **2.6998 μs** |  **0.1480 μs** | **3.050 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.021 μs |   0.6407 μs |  0.0351 μs | 2.024 μs |  0.65 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.886 μs |   8.2586 μs |  0.4527 μs | 2.666 μs |  0.93 |    0.13 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  3.860 μs |   4.4668 μs |  0.2448 μs | 3.807 μs |  1.24 |    0.08 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.432 μs |   5.6659 μs |  0.3106 μs | 6.542 μs |  2.06 |    0.12 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.083 μs |   3.5297 μs |  0.1935 μs | 2.976 μs |  0.99 |    0.07 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.337 μs |   3.7286 μs |  0.2044 μs | 3.266 μs |  1.07 |    0.07 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  5.630 μs |   3.5117 μs |  0.1925 μs | 5.720 μs |  1.81 |    0.09 |    3 |     152 B |        2.71 |
                                  |            |           |             |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **3.398 μs** |   **5.7796 μs** |  **0.3168 μs** | **3.241 μs** |  **1.01** |    **0.11** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 10.734 μs | 249.1682 μs | 13.6577 μs | 2.969 μs |  3.18 |    3.52 |    5 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.698 μs |   1.4844 μs |  0.0814 μs | 2.684 μs |  0.80 |    0.06 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  4.025 μs |   3.1193 μs |  0.1710 μs | 4.108 μs |  1.19 |    0.10 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         |  8.098 μs |   3.7425 μs |  0.2051 μs | 8.145 μs |  2.40 |    0.19 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  4.008 μs |   5.5666 μs |  0.3051 μs | 4.018 μs |  1.19 |    0.12 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  4.074 μs |   6.2779 μs |  0.3441 μs | 4.127 μs |  1.21 |    0.13 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  5.670 μs |   4.9113 μs |  0.2692 μs | 5.530 μs |  1.68 |    0.15 |    3 |     152 B |        2.71 |
