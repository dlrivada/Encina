
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.737 μs** | **10.596 μs** | **0.5808 μs** |  **1.02** |    **0.20** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.815 μs |  4.086 μs | 0.2239 μs |  0.77 |    0.12 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.824 μs |  8.355 μs | 0.4580 μs |  0.77 |    0.15 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.351 μs | 12.316 μs | 0.6751 μs |  1.18 |    0.23 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.495 μs |  4.517 μs | 0.2476 μs |  1.77 |    0.26 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.737 μs |  4.664 μs | 0.2556 μs |  1.02 |    0.16 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.606 μs |  4.087 μs | 0.2240 μs |  1.25 |    0.19 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.544 μs |  8.549 μs | 0.4686 μs |  1.78 |    0.28 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.100 μs** | **11.229 μs** | **0.6155 μs** |  **1.01** |    **0.18** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.351 μs |  4.782 μs | 0.2621 μs |  0.58 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.516 μs | 10.701 μs | 0.5866 μs |  0.87 |    0.17 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 6.243 μs |  1.934 μs | 0.1060 μs |  1.54 |    0.19 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.801 μs | 11.879 μs | 0.6511 μs |  2.43 |    0.33 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.676 μs |  5.903 μs | 0.3236 μs |  0.91 |    0.13 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.965 μs | 16.621 μs | 0.9110 μs |  1.48 |    0.27 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.388 μs |  6.031 μs | 0.3306 μs |  1.58 |    0.21 |    3 |     152 B |        2.71 |
