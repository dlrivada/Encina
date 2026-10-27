
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.661 μs** |  **4.2684 μs** | **0.2340 μs** |  **1.00** |    **0.08** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.121 μs |  1.2909 μs | 0.0708 μs |  0.58 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.662 μs |  2.4169 μs | 0.1325 μs |  0.73 |    0.05 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.828 μs |  2.2860 μs | 0.1253 μs |  1.05 |    0.07 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.416 μs |  7.9759 μs | 0.4372 μs |  1.76 |    0.14 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.661 μs |  7.6994 μs | 0.4220 μs |  1.00 |    0.12 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 5.063 μs |  6.5907 μs | 0.3613 μs |  1.39 |    0.12 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.810 μs |  4.1172 μs | 0.2257 μs |  1.59 |    0.10 |    4 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.805 μs** |  **6.5804 μs** | **0.3607 μs** |  **1.01** |    **0.12** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.867 μs | 12.1331 μs | 0.6651 μs |  0.76 |    0.16 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.733 μs |  0.4495 μs | 0.0246 μs |  0.72 |    0.06 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.832 μs |  1.6554 μs | 0.0907 μs |  1.28 |    0.11 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.881 μs |  5.6519 μs | 0.3098 μs |  2.08 |    0.19 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.500 μs |  6.4409 μs | 0.3531 μs |  0.93 |    0.11 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.122 μs |  5.0703 μs | 0.2779 μs |  1.09 |    0.11 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.183 μs | 14.3390 μs | 0.7860 μs |  1.63 |    0.22 |    3 |     152 B |        2.71 |
