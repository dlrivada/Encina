
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.511 μs** | **13.5188 μs** | **0.7410 μs** |  **1.05** |    **0.36** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.886 μs |  2.7447 μs | 0.1504 μs |  0.79 |    0.18 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.970 μs |  2.2770 μs | 0.1248 μs |  0.83 |    0.19 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.219 μs |  4.5890 μs | 0.2515 μs |  1.35 |    0.31 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 3.873 μs |  5.9343 μs | 0.3253 μs |  1.62 |    0.37 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.471 μs |  7.2617 μs | 0.3980 μs |  1.04 |    0.27 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.973 μs |  7.7771 μs | 0.4263 μs |  1.25 |    0.31 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.710 μs |  6.9769 μs | 0.3824 μs |  1.97 |    0.45 |    5 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.435 μs** |  **7.8690 μs** | **0.4313 μs** |  **1.01** |    **0.16** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.716 μs |  0.6843 μs | 0.0375 μs |  0.50 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.204 μs |  4.1224 μs | 0.2260 μs |  0.65 |    0.09 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.218 μs |  9.7611 μs | 0.5350 μs |  0.95 |    0.17 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 5.635 μs |  7.5331 μs | 0.4129 μs |  1.66 |    0.21 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 2.834 μs |  6.2850 μs | 0.3445 μs |  0.83 |    0.13 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.192 μs |  7.5331 μs | 0.4129 μs |  0.94 |    0.15 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.018 μs | 10.9936 μs | 0.6026 μs |  1.48 |    0.22 |    4 |     152 B |        2.71 |
