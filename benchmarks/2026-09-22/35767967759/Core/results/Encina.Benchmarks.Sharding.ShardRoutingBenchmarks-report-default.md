
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.299 μs** |  **1.782 μs** | **0.0977 μs** | **3.275 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.536 μs |  2.281 μs | 0.1250 μs | 2.539 μs |  0.77 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.766 μs |  4.048 μs | 0.2219 μs | 2.685 μs |  0.84 |    0.06 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.960 μs |  1.381 μs | 0.0757 μs | 3.927 μs |  1.20 |    0.04 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.978 μs |  6.241 μs | 0.3421 μs | 5.801 μs |  1.81 |    0.10 |    2 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 4.522 μs | 22.284 μs | 1.2215 μs | 3.857 μs |  1.37 |    0.32 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.296 μs |  3.529 μs | 0.1934 μs | 3.216 μs |  1.00 |    0.06 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.461 μs |  4.419 μs | 0.2422 μs | 5.600 μs |  1.66 |    0.08 |    2 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.272 μs** |  **2.894 μs** | **0.1587 μs** | **4.308 μs** |  **1.00** |    **0.05** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 3.570 μs | 24.328 μs | 1.3335 μs | 2.816 μs |  0.84 |    0.27 |    2 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.528 μs |  1.100 μs | 0.0603 μs | 2.535 μs |  0.59 |    0.02 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.908 μs |  2.586 μs | 0.1418 μs | 3.858 μs |  0.92 |    0.04 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.655 μs |  3.426 μs | 0.1878 μs | 7.625 μs |  1.79 |    0.07 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.297 μs |  2.724 μs | 0.1493 μs | 3.237 μs |  0.77 |    0.04 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.794 μs |  1.978 μs | 0.1084 μs | 3.747 μs |  0.89 |    0.04 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.474 μs |  3.874 μs | 0.2124 μs | 5.410 μs |  1.28 |    0.06 |    4 |     152 B |        2.71 |
