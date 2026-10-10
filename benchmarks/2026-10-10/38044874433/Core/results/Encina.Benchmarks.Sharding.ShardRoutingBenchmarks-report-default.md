
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.216 μs** |   **5.710 μs** |  **0.3130 μs** | **3.377 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.473 μs |   7.096 μs |  0.3889 μs | 2.590 μs |  0.77 |    0.13 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  3.097 μs |   2.651 μs |  0.1453 μs | 3.086 μs |  0.97 |    0.10 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 12.784 μs | 277.189 μs | 15.1937 μs | 4.158 μs |  4.00 |    4.15 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  7.535 μs |   1.943 μs |  0.1065 μs | 7.519 μs |  2.36 |    0.21 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.680 μs |   3.863 μs |  0.2118 μs | 3.737 μs |  1.15 |    0.12 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.564 μs |   2.955 μs |  0.1620 μs | 3.647 μs |  1.12 |    0.11 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  5.570 μs |   2.508 μs |  0.1375 μs | 5.540 μs |  1.74 |    0.16 |    2 |     152 B |        2.71 |
                                  |            |           |            |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **3.764 μs** |   **6.803 μs** |  **0.3729 μs** | **3.616 μs** |  **1.01** |    **0.12** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.206 μs |   2.601 μs |  0.1426 μs | 2.139 μs |  0.59 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.798 μs |   3.198 μs |  0.1753 μs | 2.845 μs |  0.75 |    0.07 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  4.302 μs |   4.677 μs |  0.2564 μs | 4.419 μs |  1.15 |    0.11 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         |  8.310 μs |   7.423 μs |  0.4069 μs | 8.160 μs |  2.22 |    0.20 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  3.688 μs |   2.669 μs |  0.1463 μs | 3.642 μs |  0.99 |    0.09 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  5.168 μs |  13.768 μs |  0.7547 μs | 5.043 μs |  1.38 |    0.21 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  6.526 μs |  13.377 μs |  0.7332 μs | 6.372 μs |  1.74 |    0.22 |    4 |     152 B |        2.71 |
