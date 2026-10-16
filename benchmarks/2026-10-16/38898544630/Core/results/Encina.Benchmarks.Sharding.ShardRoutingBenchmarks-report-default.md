
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.537 μs** |  **4.6848 μs** | **0.2568 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.581 μs |  9.9489 μs | 0.5453 μs |  0.73 |    0.14 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.676 μs |  4.7357 μs | 0.2596 μs |  0.76 |    0.08 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.751 μs |  2.6396 μs | 0.1447 μs |  1.06 |    0.08 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.361 μs |  5.3501 μs | 0.2933 μs |  1.80 |    0.14 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.393 μs |  0.7595 μs | 0.0416 μs |  0.96 |    0.06 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.621 μs |  6.7070 μs | 0.3676 μs |  1.03 |    0.11 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.208 μs |  7.2289 μs | 0.3962 μs |  1.76 |    0.15 |    3 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.867 μs** |  **4.1321 μs** | **0.2265 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.981 μs |  3.0237 μs | 0.1657 μs |  0.77 |    0.05 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.753 μs |  0.7625 μs | 0.0418 μs |  0.71 |    0.04 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.221 μs |  8.2795 μs | 0.4538 μs |  1.09 |    0.12 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.729 μs |  3.2990 μs | 0.1808 μs |  2.00 |    0.11 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.935 μs | 25.9153 μs | 1.4205 μs |  1.28 |    0.33 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.897 μs |  3.6547 μs | 0.2003 μs |  1.01 |    0.07 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.392 μs |  2.0665 μs | 0.1133 μs |  1.66 |    0.09 |    3 |     152 B |        2.71 |
