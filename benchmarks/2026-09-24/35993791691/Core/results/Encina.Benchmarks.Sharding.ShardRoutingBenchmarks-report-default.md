
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.477 μs** |  **4.501 μs** | **0.2467 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.754 μs |  2.930 μs | 0.1606 μs |  0.79 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 3.567 μs |  8.252 μs | 0.4523 μs |  1.03 |    0.13 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 5.587 μs | 16.417 μs | 0.8999 μs |  1.61 |    0.25 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.211 μs |  6.503 μs | 0.3564 μs |  1.79 |    0.15 |    2 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.171 μs |  4.324 μs | 0.2370 μs |  0.92 |    0.08 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.379 μs |  3.633 μs | 0.1991 μs |  0.98 |    0.08 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.714 μs |  4.806 μs | 0.2634 μs |  1.65 |    0.12 |    2 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.216 μs** |  **6.866 μs** | **0.3764 μs** |  **1.01** |    **0.11** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.321 μs |  3.552 μs | 0.1947 μs |  0.55 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.575 μs |  1.853 μs | 0.1016 μs |  0.61 |    0.05 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.208 μs | 11.012 μs | 0.6036 μs |  1.00 |    0.15 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.115 μs | 11.378 μs | 0.6237 μs |  1.93 |    0.19 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.668 μs |  4.986 μs | 0.2733 μs |  1.11 |    0.10 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.897 μs |  3.713 μs | 0.2035 μs |  0.93 |    0.08 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 7.605 μs | 28.072 μs | 1.5387 μs |  1.81 |    0.35 |    3 |     152 B |        2.71 |
