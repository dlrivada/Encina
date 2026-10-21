
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.451 μs** |  **9.098 μs** | **0.4987 μs** |  **1.01** |    **0.18** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.254 μs |  3.435 μs | 0.1883 μs |  0.66 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  3.336 μs |  6.998 μs | 0.3836 μs |  0.98 |    0.15 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  4.031 μs |  5.875 μs | 0.3220 μs |  1.18 |    0.17 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.185 μs |  6.209 μs | 0.3403 μs |  1.82 |    0.24 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.308 μs |  3.938 μs | 0.2159 μs |  0.97 |    0.13 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.397 μs |  7.436 μs | 0.4076 μs |  1.00 |    0.16 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  6.311 μs |  9.319 μs | 0.5108 μs |  1.85 |    0.26 |    3 |     152 B |        2.71 |
                                  |            |           |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **4.061 μs** |  **6.294 μs** | **0.3450 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  3.002 μs |  9.920 μs | 0.5438 μs |  0.74 |    0.13 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  3.210 μs |  7.103 μs | 0.3893 μs |  0.79 |    0.10 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  4.565 μs | 15.651 μs | 0.8579 μs |  1.13 |    0.20 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 10.667 μs | 31.103 μs | 1.7049 μs |  2.64 |    0.41 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  3.811 μs | 11.979 μs | 0.6566 μs |  0.94 |    0.16 |    1 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  4.057 μs |  6.641 μs | 0.3640 μs |  1.00 |    0.11 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  5.634 μs |  6.731 μs | 0.3690 μs |  1.39 |    0.13 |    2 |     152 B |        2.71 |
