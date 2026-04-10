
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.990 μs** | **3.6606 μs** | **0.2007 μs** |  **1.00** |    **0.08** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.004 μs | 2.1127 μs | 0.1158 μs |  0.67 |    0.05 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.418 μs | 2.0378 μs | 0.1117 μs |  0.81 |    0.06 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.231 μs | 5.8790 μs | 0.3222 μs |  1.42 |    0.12 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.983 μs | 3.8120 μs | 0.2090 μs |  2.01 |    0.13 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.033 μs | 1.3198 μs | 0.0723 μs |  1.02 |    0.06 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.169 μs | 2.1717 μs | 0.1190 μs |  1.06 |    0.07 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.104 μs | 3.4599 μs | 0.1896 μs |  2.05 |    0.13 |    5 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.400 μs** | **2.8375 μs** | **0.1555 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.120 μs | 1.7429 μs | 0.0955 μs |  0.62 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.558 μs | 0.9043 μs | 0.0496 μs |  0.75 |    0.03 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.925 μs | 2.6079 μs | 0.1429 μs |  1.16 |    0.06 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.564 μs | 2.5736 μs | 0.1411 μs |  2.23 |    0.10 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.169 μs | 1.8739 μs | 0.1027 μs |  0.93 |    0.05 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.619 μs | 2.9457 μs | 0.1615 μs |  1.36 |    0.07 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.426 μs | 2.5968 μs | 0.1423 μs |  1.60 |    0.07 |    2 |     152 B |        2.71 |
