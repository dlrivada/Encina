
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.942 μs** |  **5.870 μs** | **0.3218 μs** |  **1.00** |    **0.10** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.613 μs | 10.786 μs | 0.5912 μs |  0.67 |    0.14 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.607 μs |  9.616 μs | 0.5271 μs |  0.66 |    0.13 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  4.321 μs |  6.704 μs | 0.3675 μs |  1.10 |    0.11 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.457 μs | 15.622 μs | 0.8563 μs |  1.65 |    0.22 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.233 μs |  2.959 μs | 0.1622 μs |  0.82 |    0.07 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.693 μs |  4.180 μs | 0.2291 μs |  0.94 |    0.08 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  6.252 μs |  9.759 μs | 0.5349 μs |  1.59 |    0.16 |    3 |     152 B |        2.71 |
                                  |            |           |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **5.626 μs** | **25.374 μs** | **1.3909 μs** |  **1.04** |    **0.30** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.478 μs |  2.566 μs | 0.1407 μs |  0.46 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.803 μs |  4.140 μs | 0.2269 μs |  0.52 |    0.11 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  4.405 μs |  8.666 μs | 0.4750 μs |  0.81 |    0.18 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 10.046 μs | 38.675 μs | 2.1199 μs |  1.85 |    0.50 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  3.502 μs |  2.061 μs | 0.1130 μs |  0.65 |    0.13 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  4.078 μs |  4.155 μs | 0.2277 μs |  0.75 |    0.15 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  8.206 μs | 25.428 μs | 1.3938 μs |  1.51 |    0.37 |    3 |     152 B |        2.71 |
