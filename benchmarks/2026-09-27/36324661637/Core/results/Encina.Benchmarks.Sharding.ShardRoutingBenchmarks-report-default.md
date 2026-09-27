
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error       | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|------------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **2.983 μs** |   **3.8648 μs** |  **0.2118 μs** | **2.876 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  1.956 μs |   1.5584 μs |  0.0854 μs | 1.923 μs |  0.66 |    0.05 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.852 μs |   6.4070 μs |  0.3512 μs | 2.885 μs |  0.96 |    0.12 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  3.834 μs |   4.9699 μs |  0.2724 μs | 3.698 μs |  1.29 |    0.11 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.145 μs |   9.2315 μs |  0.5060 μs | 6.142 μs |  2.07 |    0.19 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.152 μs |   1.9564 μs |  0.1072 μs | 3.096 μs |  1.06 |    0.07 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.307 μs |   3.0761 μs |  0.1686 μs | 3.247 μs |  1.11 |    0.08 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  5.334 μs |   6.5993 μs |  0.3617 μs | 5.140 μs |  1.79 |    0.15 |    3 |     152 B |        2.71 |
                                  |            |           |             |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **3.305 μs** |   **4.7286 μs** |  **0.2592 μs** | **3.255 μs** |  **1.00** |    **0.10** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.143 μs |   0.3770 μs |  0.0207 μs | 2.149 μs |  0.65 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.678 μs |   3.5045 μs |  0.1921 μs | 2.645 μs |  0.81 |    0.07 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  3.847 μs |   2.8110 μs |  0.1541 μs | 3.806 μs |  1.17 |    0.09 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         |  7.628 μs |   5.8379 μs |  0.3200 μs | 7.755 μs |  2.32 |    0.18 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  3.637 μs |  16.1636 μs |  0.8860 μs | 3.166 μs |  1.10 |    0.24 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  5.323 μs |   8.7778 μs |  0.4811 μs | 5.336 μs |  1.62 |    0.17 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 14.628 μs | 288.0982 μs | 15.7916 μs | 5.671 μs |  4.44 |    4.17 |    6 |     152 B |        2.71 |
