
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.531 μs** |  **3.878 μs** | **0.2126 μs** |  **1.00** |    **0.07** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.124 μs |  4.565 μs | 0.2502 μs |  0.60 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.921 μs | 17.782 μs | 0.9747 μs |  0.83 |    0.24 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.979 μs |  6.921 μs | 0.3793 μs |  1.13 |    0.11 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.575 μs |  5.221 μs | 0.2862 μs |  1.58 |    0.11 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.265 μs |  5.744 μs | 0.3149 μs |  0.93 |    0.09 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.466 μs |  8.489 μs | 0.4653 μs |  0.98 |    0.12 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.865 μs |  7.964 μs | 0.4365 μs |  1.66 |    0.14 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.646 μs** |  **3.640 μs** | **0.1995 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.364 μs |  2.230 μs | 0.1222 μs |  0.65 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.844 μs |  2.391 μs | 0.1311 μs |  0.78 |    0.05 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.536 μs |  8.646 μs | 0.4739 μs |  1.25 |    0.13 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.426 μs |  6.577 μs | 0.3605 μs |  2.04 |    0.13 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.462 μs |  8.902 μs | 0.4880 μs |  0.95 |    0.12 |    1 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.691 μs |  5.957 μs | 0.3265 μs |  1.29 |    0.10 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.937 μs |  9.917 μs | 0.5436 μs |  1.63 |    0.15 |    3 |     152 B |        2.71 |
