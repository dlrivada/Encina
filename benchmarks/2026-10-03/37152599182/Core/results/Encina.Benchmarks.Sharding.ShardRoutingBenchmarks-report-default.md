
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error       | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|------------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.060 μs** |   **3.6618 μs** | **0.2007 μs** | **2.987 μs** |  **1.00** |    **0.08** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.897 μs |   1.8274 μs | 0.1002 μs | 1.933 μs |  0.62 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.552 μs |   2.4295 μs | 0.1332 μs | 2.485 μs |  0.84 |    0.06 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.780 μs |   4.3211 μs | 0.2369 μs | 3.696 μs |  1.24 |    0.10 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.008 μs |   4.9912 μs | 0.2736 μs | 5.880 μs |  1.97 |    0.13 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 8.744 μs | 176.2469 μs | 9.6607 μs | 3.287 μs |  2.87 |    2.75 |    5 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.296 μs |   6.4964 μs | 0.3561 μs | 3.176 μs |  1.08 |    0.12 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.424 μs |   3.1444 μs | 0.1724 μs | 5.350 μs |  1.78 |    0.11 |    4 |     152 B |        2.71 |
                                  |            |          |             |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.544 μs** |   **3.2856 μs** | **0.1801 μs** | **3.627 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.188 μs |   0.7698 μs | 0.0422 μs | 2.174 μs |  0.62 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.525 μs |   0.8360 μs | 0.0458 μs | 2.534 μs |  0.71 |    0.03 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.824 μs |   2.9085 μs | 0.1594 μs | 3.737 μs |  1.08 |    0.06 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.832 μs |   7.7525 μs | 0.4249 μs | 7.765 μs |  2.21 |    0.14 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.195 μs |   2.6974 μs | 0.1479 μs | 3.165 μs |  0.90 |    0.05 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.931 μs |   1.3448 μs | 0.0737 μs | 3.958 μs |  1.11 |    0.05 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.524 μs |   4.0855 μs | 0.2239 μs | 5.410 μs |  1.56 |    0.09 |    3 |     152 B |        2.71 |
