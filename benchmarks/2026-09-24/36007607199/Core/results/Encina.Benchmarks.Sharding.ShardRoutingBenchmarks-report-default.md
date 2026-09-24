
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.270 μs** |  **4.944 μs** | **0.2710 μs** |  **1.01** |    **0.14** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.850 μs |  3.727 μs | 0.2043 μs |  0.82 |    0.11 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.375 μs |  6.662 μs | 0.3652 μs |  1.06 |    0.18 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.178 μs |  7.778 μs | 0.4264 μs |  1.41 |    0.22 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 4.036 μs | 11.817 μs | 0.6477 μs |  1.79 |    0.31 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.654 μs |  3.476 μs | 0.1906 μs |  1.18 |    0.14 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.878 μs |  3.138 μs | 0.1720 μs |  1.28 |    0.14 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.724 μs |  4.936 μs | 0.2706 μs |  2.10 |    0.23 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.165 μs** |  **7.834 μs** | **0.4294 μs** |  **1.01** |    **0.17** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.770 μs |  6.926 μs | 0.3796 μs |  0.57 |    0.13 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.184 μs |  5.061 μs | 0.2774 μs |  0.70 |    0.12 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.561 μs |  6.238 μs | 0.3419 μs |  1.14 |    0.17 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 6.218 μs |  9.588 μs | 0.5256 μs |  1.99 |    0.29 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 2.774 μs |  2.545 μs | 0.1395 μs |  0.89 |    0.12 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.273 μs |  3.793 μs | 0.2079 μs |  1.05 |    0.14 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.670 μs | 19.983 μs | 1.0953 μs |  1.82 |    0.38 |    5 |     152 B |        2.71 |
