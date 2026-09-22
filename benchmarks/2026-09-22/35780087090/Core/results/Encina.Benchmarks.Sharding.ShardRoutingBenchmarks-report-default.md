
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.454 μs** |  **4.937 μs** | **0.2706 μs** |  **1.00** |    **0.10** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.953 μs | 19.672 μs | 1.0783 μs |  0.86 |    0.28 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 3.509 μs |  9.763 μs | 0.5351 μs |  1.02 |    0.15 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.044 μs |  7.341 μs | 0.4024 μs |  1.18 |    0.13 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.275 μs |  6.774 μs | 0.3713 μs |  1.82 |    0.16 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.491 μs |  7.865 μs | 0.4311 μs |  1.02 |    0.13 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.502 μs |  5.262 μs | 0.2884 μs |  1.02 |    0.10 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.301 μs |  2.072 μs | 0.1136 μs |  1.83 |    0.13 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.694 μs** |  **6.170 μs** | **0.3382 μs** |  **1.01** |    **0.12** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.832 μs |  1.561 μs | 0.0856 μs |  0.77 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.651 μs |  3.220 μs | 0.1765 μs |  0.72 |    0.07 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.305 μs |  6.331 μs | 0.3470 μs |  1.17 |    0.13 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.385 μs |  8.071 μs | 0.4424 μs |  2.56 |    0.24 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 5.257 μs | 28.819 μs | 1.5797 μs |  1.43 |    0.39 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.835 μs |  4.226 μs | 0.2316 μs |  1.04 |    0.10 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.754 μs |  5.513 μs | 0.3022 μs |  1.57 |    0.15 |    3 |     152 B |        2.71 |
