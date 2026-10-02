
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.132 μs** |  **6.586 μs** | **0.3610 μs** |  **1.01** |    **0.14** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.948 μs |  2.424 μs | 0.1329 μs |  0.63 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.791 μs |  3.673 μs | 0.2013 μs |  0.90 |    0.10 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.999 μs |  8.965 μs | 0.4914 μs |  1.29 |    0.18 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.282 μs |  4.469 μs | 0.2449 μs |  1.70 |    0.17 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.138 μs |  9.007 μs | 0.4937 μs |  1.01 |    0.17 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.342 μs |  9.596 μs | 0.5260 μs |  1.08 |    0.18 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.879 μs |  6.970 μs | 0.3821 μs |  1.89 |    0.21 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.833 μs** | **11.664 μs** | **0.6393 μs** |  **1.02** |    **0.20** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.326 μs |  3.286 μs | 0.1801 μs |  0.62 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.035 μs |  8.223 μs | 0.4507 μs |  0.81 |    0.15 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.443 μs |  8.153 μs | 0.4469 μs |  1.18 |    0.19 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 6.727 μs |  8.235 μs | 0.4514 μs |  1.79 |    0.26 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.177 μs | 10.709 μs | 0.5870 μs |  1.11 |    0.20 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.440 μs |  7.703 μs | 0.4222 μs |  1.18 |    0.19 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.236 μs |  6.927 μs | 0.3797 μs |  1.66 |    0.24 |    4 |     152 B |        2.71 |
