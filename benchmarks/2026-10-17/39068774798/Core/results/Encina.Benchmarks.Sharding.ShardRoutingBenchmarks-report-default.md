
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.142 μs** |  **3.5015 μs** | **0.1919 μs** | **3.115 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.375 μs |  5.3199 μs | 0.2916 μs | 2.265 μs |  0.76 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.602 μs |  2.0013 μs | 0.1097 μs | 2.565 μs |  0.83 |    0.05 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.546 μs |  2.9781 μs | 0.1632 μs | 4.619 μs |  1.45 |    0.09 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.138 μs |  8.6913 μs | 0.4764 μs | 6.021 μs |  1.96 |    0.17 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.191 μs |  1.5673 μs | 0.0859 μs | 3.182 μs |  1.02 |    0.06 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.373 μs |  0.6907 μs | 0.0379 μs | 3.356 μs |  1.08 |    0.06 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.387 μs |  2.9095 μs | 0.1595 μs | 5.310 μs |  1.72 |    0.10 |    3 |     152 B |        2.71 |
                                  |            |          |            |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.806 μs** | **10.0718 μs** | **0.5521 μs** | **3.656 μs** |  **1.01** |    **0.18** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 3.683 μs | 27.3652 μs | 1.5000 μs | 2.945 μs |  0.98 |    0.37 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.562 μs |  0.9463 μs | 0.0519 μs | 2.575 μs |  0.68 |    0.08 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.001 μs |  2.8400 μs | 0.1557 μs | 4.018 μs |  1.07 |    0.13 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.000 μs | 30.6217 μs | 1.6785 μs | 8.425 μs |  2.40 |    0.48 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.859 μs |  3.0133 μs | 0.1652 μs | 3.812 μs |  1.03 |    0.13 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.951 μs |  3.0437 μs | 0.1668 μs | 3.927 μs |  1.05 |    0.13 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.590 μs |  3.5234 μs | 0.1931 μs | 5.550 μs |  1.49 |    0.18 |    3 |     152 B |        2.71 |
