
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.457 μs** |  **4.4337 μs** | **0.2430 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.105 μs |  0.7011 μs | 0.0384 μs |  0.61 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 3.447 μs |  2.5632 μs | 0.1405 μs |  1.00 |    0.07 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.820 μs | 10.9202 μs | 0.5986 μs |  1.40 |    0.17 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.532 μs |  3.1652 μs | 0.1735 μs |  1.90 |    0.13 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 4.275 μs |  5.2051 μs | 0.2853 μs |  1.24 |    0.11 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.601 μs |  2.0096 μs | 0.1102 μs |  1.34 |    0.09 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.694 μs |  9.4695 μs | 0.5191 μs |  1.94 |    0.18 |    4 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.866 μs** |  **8.0980 μs** | **0.4439 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 3.436 μs |  9.9495 μs | 0.5454 μs |  0.90 |    0.15 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.032 μs |  1.1729 μs | 0.0643 μs |  0.79 |    0.08 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.790 μs |  2.2069 μs | 0.1210 μs |  1.25 |    0.12 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.748 μs | 11.5139 μs | 0.6311 μs |  2.28 |    0.26 |    2 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.450 μs |  4.8635 μs | 0.2666 μs |  0.90 |    0.10 |    1 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.041 μs |  4.2290 μs | 0.2318 μs |  1.05 |    0.11 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.761 μs |  7.4369 μs | 0.4076 μs |  1.50 |    0.17 |    1 |     152 B |        2.71 |
