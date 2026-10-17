
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error       | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|------------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.233 μs** |   **3.8506 μs** |  **0.2111 μs** | **3.227 μs** |  **1.00** |    **0.08** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.045 μs |   0.7206 μs |  0.0395 μs | 2.045 μs |  0.63 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.599 μs |   1.7337 μs |  0.0950 μs | 2.596 μs |  0.81 |    0.05 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.824 μs |   3.4326 μs |  0.1881 μs | 3.788 μs |  1.19 |    0.08 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.241 μs |   5.0222 μs |  0.2753 μs | 6.182 μs |  1.94 |    0.13 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 4.015 μs |   2.1295 μs |  0.1167 μs | 4.073 μs |  1.25 |    0.08 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.238 μs |   4.8061 μs |  0.2634 μs | 3.102 μs |  1.00 |    0.09 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.568 μs |   3.8715 μs |  0.2122 μs | 5.471 μs |  1.73 |    0.11 |    4 |     152 B |        2.71 |
                                  |            |          |             |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.539 μs** |   **3.2448 μs** |  **0.1779 μs** | **3.576 μs** |  **1.00** |    **0.06** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.180 μs |   2.4939 μs |  0.1367 μs | 2.133 μs |  0.62 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.658 μs |   1.2943 μs |  0.0709 μs | 2.645 μs |  0.75 |    0.04 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.971 μs |   2.2140 μs |  0.1214 μs | 3.928 μs |  1.12 |    0.06 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.795 μs |   5.8801 μs |  0.3223 μs | 7.635 μs |  2.21 |    0.13 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.443 μs |   2.0987 μs |  0.1150 μs | 3.446 μs |  0.97 |    0.05 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 9.879 μs | 188.2337 μs | 10.3177 μs | 4.048 μs |  2.80 |    2.53 |    6 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.699 μs |   3.6021 μs |  0.1974 μs | 5.596 μs |  1.61 |    0.09 |    4 |     152 B |        2.71 |
