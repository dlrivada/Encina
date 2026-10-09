
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.324 μs** |  **4.733 μs** | **0.2594 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.893 μs |  4.909 μs | 0.2691 μs |  0.82 |    0.13 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.075 μs |  3.214 μs | 0.1762 μs |  0.90 |    0.11 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.051 μs | 15.558 μs | 0.8528 μs |  1.32 |    0.34 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 3.879 μs |  9.202 μs | 0.5044 μs |  1.68 |    0.25 |    2 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.694 μs |  6.244 μs | 0.3422 μs |  1.17 |    0.17 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.880 μs | 15.801 μs | 0.8661 μs |  1.25 |    0.35 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.290 μs | 10.799 μs | 0.5919 μs |  1.86 |    0.28 |    2 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **2.855 μs** |  **6.421 μs** | **0.3519 μs** |  **1.01** |    **0.15** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.993 μs |  4.134 μs | 0.2266 μs |  0.71 |    0.10 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.581 μs |  9.550 μs | 0.5235 μs |  0.91 |    0.19 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.930 μs | 14.930 μs | 0.8183 μs |  1.39 |    0.29 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 5.961 μs |  3.020 μs | 0.1655 μs |  2.11 |    0.22 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.679 μs | 13.278 μs | 0.7278 μs |  1.30 |    0.26 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.274 μs |  8.526 μs | 0.4674 μs |  1.16 |    0.19 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 4.900 μs | 15.038 μs | 0.8243 μs |  1.73 |    0.31 |    3 |     152 B |        2.71 |
