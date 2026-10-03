
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.539 μs** |   **8.469 μs** | **0.4642 μs** | **3.648 μs** |  **1.01** |    **0.17** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  3.836 μs |   7.362 μs | 0.4035 μs | 3.795 μs |  1.10 |    0.17 |    2 |      48 B |        0.86 |
 'Directory routing'              | 3          |  3.568 μs |  28.577 μs | 1.5664 μs | 2.834 μs |  1.02 |    0.41 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  4.348 μs |  10.057 μs | 0.5513 μs | 4.064 μs |  1.24 |    0.20 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  5.555 μs |   9.560 μs | 0.5240 μs | 5.529 μs |  1.59 |    0.23 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  5.683 μs |   5.329 μs | 0.2921 μs | 5.620 μs |  1.63 |    0.21 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.842 μs |   8.483 μs | 0.4650 μs | 3.719 μs |  1.10 |    0.18 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  7.102 μs |  10.528 μs | 0.5771 μs | 7.398 μs |  2.03 |    0.28 |    4 |     152 B |        2.71 |
                                  |            |           |            |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **3.725 μs** |   **6.950 μs** | **0.3809 μs** | **3.542 μs** |  **1.01** |    **0.12** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.260 μs |   5.772 μs | 0.3164 μs | 2.097 μs |  0.61 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  3.414 μs |   4.209 μs | 0.2307 μs | 3.358 μs |  0.92 |    0.09 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  4.615 μs |   9.070 μs | 0.4971 μs | 4.451 μs |  1.25 |    0.16 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         |  9.086 μs |  13.683 μs | 0.7500 μs | 9.282 μs |  2.45 |    0.27 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  4.352 μs |  10.201 μs | 0.5592 μs | 4.119 μs |  1.18 |    0.16 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 10.771 μs | 150.995 μs | 8.2766 μs | 6.128 μs |  2.91 |    1.96 |    5 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  6.648 μs |  21.123 μs | 1.1578 μs | 6.157 μs |  1.80 |    0.31 |    3 |     152 B |        2.71 |
