
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.437 μs** |  **9.924 μs** | **0.5440 μs** |  **1.03** |    **0.27** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.536 μs |  2.863 μs | 0.1570 μs |  0.65 |    0.13 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.772 μs |  3.113 μs | 0.1706 μs |  0.75 |    0.14 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.476 μs | 11.622 μs | 0.6370 μs |  1.47 |    0.35 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 3.884 μs |  8.877 μs | 0.4866 μs |  1.64 |    0.33 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.607 μs |  2.027 μs | 0.1111 μs |  1.10 |    0.19 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.501 μs | 11.841 μs | 0.6490 μs |  1.06 |    0.30 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.260 μs | 10.191 μs | 0.5586 μs |  1.80 |    0.37 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **2.895 μs** | **13.769 μs** | **0.7547 μs** |  **1.04** |    **0.31** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.733 μs |  1.493 μs | 0.0819 μs |  0.62 |    0.13 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.581 μs |  7.330 μs | 0.4018 μs |  0.93 |    0.22 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.145 μs |  2.735 μs | 0.1499 μs |  1.13 |    0.23 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 5.657 μs |  6.471 μs | 0.3547 μs |  2.03 |    0.41 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.276 μs |  2.935 μs | 0.1609 μs |  1.18 |    0.24 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.148 μs |  4.529 μs | 0.2483 μs |  1.13 |    0.24 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 4.741 μs |  8.679 μs | 0.4757 μs |  1.70 |    0.37 |    4 |     152 B |        2.71 |
