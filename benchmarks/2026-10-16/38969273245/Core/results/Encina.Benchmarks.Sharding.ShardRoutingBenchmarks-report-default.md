
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.432 μs** |  **2.9866 μs** | **0.1637 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.459 μs |  1.3257 μs | 0.0727 μs |  0.72 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.505 μs |  0.7297 μs | 0.0400 μs |  0.73 |    0.03 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.885 μs |  2.1145 μs | 0.1159 μs |  1.13 |    0.06 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.077 μs |  5.6773 μs | 0.3112 μs |  1.77 |    0.11 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.199 μs |  4.7057 μs | 0.2579 μs |  0.93 |    0.08 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.215 μs |  2.8316 μs | 0.1552 μs |  0.94 |    0.06 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.462 μs |  4.4985 μs | 0.2466 μs |  1.59 |    0.09 |    3 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.570 μs** |  **2.3166 μs** | **0.1270 μs** |  **1.00** |    **0.04** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.605 μs |  8.8562 μs | 0.4854 μs |  0.73 |    0.12 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.656 μs |  1.6287 μs | 0.0893 μs |  0.74 |    0.03 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.810 μs |  2.1717 μs | 0.1190 μs |  1.07 |    0.04 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.980 μs | 38.2981 μs | 2.0992 μs |  2.52 |    0.52 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.373 μs |  5.2577 μs | 0.2882 μs |  0.95 |    0.08 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.820 μs |  2.2903 μs | 0.1255 μs |  1.07 |    0.04 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.434 μs |  3.4387 μs | 0.1885 μs |  1.52 |    0.07 |    3 |     152 B |        2.71 |
