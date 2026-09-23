
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.266 μs** |  **7.022 μs** | **0.3849 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 3.300 μs | 23.156 μs | 1.2692 μs |  1.02 |    0.36 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.741 μs |  1.014 μs | 0.0556 μs |  0.85 |    0.08 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.897 μs |  3.211 μs | 0.1760 μs |  1.20 |    0.13 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.652 μs | 12.160 μs | 0.6665 μs |  2.05 |    0.27 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.386 μs |  4.458 μs | 0.2443 μs |  1.05 |    0.12 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.077 μs | 11.581 μs | 0.6348 μs |  1.26 |    0.21 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.557 μs |  3.865 μs | 0.2118 μs |  1.72 |    0.18 |    2 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.427 μs** |  **2.151 μs** | **0.1179 μs** |  **1.00** |    **0.04** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.640 μs |  2.819 μs | 0.1545 μs |  0.77 |    0.05 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.537 μs | 10.200 μs | 0.5591 μs |  1.03 |    0.14 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.860 μs |  5.258 μs | 0.2882 μs |  1.13 |    0.08 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.507 μs | 19.851 μs | 1.0881 μs |  2.48 |    0.29 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 5.415 μs | 33.688 μs | 1.8466 μs |  1.58 |    0.47 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.864 μs |  7.558 μs | 0.4143 μs |  1.13 |    0.11 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.728 μs |  4.859 μs | 0.2663 μs |  1.67 |    0.08 |    3 |     152 B |        2.71 |
