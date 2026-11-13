
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.309 μs** |  **8.429 μs** | **0.4620 μs** |  **1.01** |    **0.17** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.071 μs |  4.185 μs | 0.2294 μs |  0.63 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.611 μs |  1.425 μs | 0.0781 μs |  0.80 |    0.09 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.784 μs |  3.666 μs | 0.2009 μs |  1.16 |    0.14 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.161 μs |  5.923 μs | 0.3246 μs |  1.88 |    0.23 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.224 μs |  2.961 μs | 0.1623 μs |  0.99 |    0.12 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.362 μs |  6.694 μs | 0.3669 μs |  1.03 |    0.15 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.374 μs |  3.696 μs | 0.2026 μs |  1.64 |    0.19 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.211 μs** | **10.069 μs** | **0.5519 μs** |  **1.01** |    **0.17** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.470 μs |  2.809 μs | 0.1539 μs |  0.59 |    0.08 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.630 μs |  3.672 μs | 0.2013 μs |  0.63 |    0.09 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.900 μs |  1.381 μs | 0.0757 μs |  0.94 |    0.12 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.074 μs | 23.002 μs | 1.2608 μs |  2.18 |    0.38 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.844 μs | 15.860 μs | 0.8693 μs |  0.92 |    0.21 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.142 μs |  8.806 μs | 0.4827 μs |  1.00 |    0.16 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 8.933 μs | 21.956 μs | 1.2035 μs |  2.15 |    0.36 |    3 |     152 B |        2.71 |
