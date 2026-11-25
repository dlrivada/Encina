
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.68GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.595 μs** |  **6.089 μs** | **0.3338 μs** |  **1.01** |    **0.11** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.966 μs |  6.115 μs | 0.3352 μs |  0.55 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.833 μs |  9.225 μs | 0.5057 μs |  0.79 |    0.14 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 5.644 μs | 19.907 μs | 1.0912 μs |  1.58 |    0.29 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 8.279 μs | 25.200 μs | 1.3813 μs |  2.32 |    0.38 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.467 μs |  4.644 μs | 0.2545 μs |  0.97 |    0.10 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 5.627 μs | 11.671 μs | 0.6397 μs |  1.57 |    0.20 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.754 μs | 13.901 μs | 0.7620 μs |  1.61 |    0.22 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **5.119 μs** |  **1.954 μs** | **0.1071 μs** |  **1.00** |    **0.03** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.321 μs |  5.476 μs | 0.3002 μs |  0.45 |    0.05 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.496 μs |  8.484 μs | 0.4650 μs |  0.68 |    0.08 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.827 μs |  8.786 μs | 0.4816 μs |  0.94 |    0.08 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.269 μs | 15.043 μs | 0.8246 μs |  1.62 |    0.14 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.126 μs |  8.568 μs | 0.4696 μs |  0.81 |    0.08 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.240 μs |  5.059 μs | 0.2773 μs |  1.02 |    0.05 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 9.452 μs | 13.037 μs | 0.7146 μs |  1.85 |    0.13 |    3 |     152 B |        2.71 |
