
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **1.880 μs** | **0.1711 μs** | **0.2399 μs** |  **1.01** |    **0.17** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.348 μs | 0.0737 μs | 0.1009 μs |  0.73 |    0.10 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.563 μs | 0.5497 μs | 0.7706 μs |  1.38 |    0.44 |    3 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 2.433 μs | 0.2769 μs | 0.3697 μs |  1.31 |    0.24 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.993 μs | 0.3049 μs | 0.4173 μs |  3.23 |    0.42 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.683 μs | 0.5873 μs | 0.8609 μs |  1.45 |    0.49 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.082 μs | 0.4168 μs | 0.5706 μs |  1.66 |    0.35 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 3.310 μs | 0.1435 μs | 0.1965 μs |  1.78 |    0.22 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.227 μs** | **0.1403 μs** | **0.1921 μs** |  **1.00** |    **0.08** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.236 μs | 0.2148 μs | 0.3081 μs |  0.70 |    0.10 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.546 μs | 0.6756 μs | 1.0112 μs |  1.10 |    0.32 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.426 μs | 0.8820 μs | 1.1774 μs |  1.38 |    0.37 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.616 μs | 1.1731 μs | 1.6445 μs |  2.99 |    0.53 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.538 μs | 1.0854 μs | 1.5567 μs |  1.41 |    0.48 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.789 μs | 0.3278 μs | 0.4702 μs |  1.18 |    0.16 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.152 μs | 0.4473 μs | 0.6270 μs |  1.91 |    0.22 |    3 |     152 B |        2.71 |
