
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.284 μs** |  **6.141 μs** | **0.3366 μs** |  **1.01** |    **0.13** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.068 μs |  3.319 μs | 0.1819 μs |  0.63 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.856 μs |  6.825 μs | 0.3741 μs |  0.88 |    0.13 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.045 μs |  8.601 μs | 0.4714 μs |  1.24 |    0.17 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 7.651 μs | 11.876 μs | 0.6510 μs |  2.35 |    0.27 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 4.159 μs |  9.003 μs | 0.4935 μs |  1.28 |    0.17 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.590 μs |  5.773 μs | 0.3164 μs |  1.10 |    0.13 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.602 μs |  8.285 μs | 0.4541 μs |  1.72 |    0.20 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.225 μs** |  **1.392 μs** | **0.0763 μs** |  **1.00** |    **0.02** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.347 μs |  5.486 μs | 0.3007 μs |  0.56 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.638 μs |  5.670 μs | 0.3108 μs |  0.62 |    0.06 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.348 μs |  7.495 μs | 0.4109 μs |  1.03 |    0.09 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.359 μs | 13.237 μs | 0.7256 μs |  2.22 |    0.15 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.550 μs |  4.532 μs | 0.2484 μs |  0.84 |    0.05 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.910 μs |  4.810 μs | 0.2637 μs |  0.93 |    0.06 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.541 μs |  3.171 μs | 0.1738 μs |  1.31 |    0.04 |    3 |     152 B |        2.71 |
