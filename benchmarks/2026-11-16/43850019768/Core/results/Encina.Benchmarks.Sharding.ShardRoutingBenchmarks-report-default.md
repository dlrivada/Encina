
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.173 μs** |  **2.1839 μs** | **0.1197 μs** |  **1.00** |    **0.05** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.951 μs |  1.4113 μs | 0.0774 μs |  0.62 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.548 μs |  0.6948 μs | 0.0381 μs |  0.80 |    0.03 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.937 μs |  1.1038 μs | 0.0605 μs |  1.24 |    0.04 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 7.230 μs | 13.8934 μs | 0.7615 μs |  2.28 |    0.22 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.183 μs |  2.0115 μs | 0.1103 μs |  1.00 |    0.04 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.310 μs |  2.6649 μs | 0.1461 μs |  1.04 |    0.05 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.458 μs |  3.2856 μs | 0.1801 μs |  1.72 |    0.07 |    5 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.506 μs** |  **1.4481 μs** | **0.0794 μs** |  **1.00** |    **0.03** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.251 μs |  2.2618 μs | 0.1240 μs |  0.64 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.771 μs |  2.7493 μs | 0.1507 μs |  0.79 |    0.04 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.047 μs |  1.6511 μs | 0.0905 μs |  1.15 |    0.03 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.258 μs | 10.1134 μs | 0.5544 μs |  2.36 |    0.14 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.699 μs |  5.6519 μs | 0.3098 μs |  1.06 |    0.08 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.958 μs |  1.5587 μs | 0.0854 μs |  1.13 |    0.03 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.955 μs | 13.8620 μs | 0.7598 μs |  1.70 |    0.19 |    4 |     152 B |        2.71 |
