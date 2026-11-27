
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.143 μs** | **20.045 μs** | **1.0987 μs** | **2.5190 μs** |  **1.07** |    **0.43** |    **5** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.149 μs | 18.174 μs | 0.9962 μs | 0.7410 μs |  0.39 |    0.32 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.958 μs | 19.617 μs | 1.0753 μs | 1.6280 μs |  0.67 |    0.37 |    3 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.052 μs | 26.520 μs | 1.4537 μs | 2.3040 μs |  1.04 |    0.51 |    5 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 3.180 μs | 31.263 μs | 1.7136 μs | 2.4790 μs |  1.09 |    0.59 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.215 μs | 20.979 μs | 1.1499 μs | 1.6180 μs |  0.76 |    0.40 |    4 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 1.875 μs | 32.958 μs | 1.8065 μs | 0.8365 μs |  0.64 |    0.57 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 3.060 μs | 39.352 μs | 2.1570 μs | 2.6190 μs |  1.04 |    0.71 |    5 |     152 B |        2.71 |
                                  |            |          |           |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.651 μs** | **36.070 μs** | **1.9771 μs** | **3.6060 μs** |  **1.11** |    **0.54** |    **5** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.572 μs | 16.146 μs | 0.8850 μs | 1.0810 μs |  0.37 |    0.22 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 4.280 μs |  3.118 μs | 0.1709 μs | 4.2660 μs |  1.02 |    0.30 |    5 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 2.412 μs | 27.639 μs | 1.5150 μs | 1.7780 μs |  0.57 |    0.37 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 6.495 μs | 37.221 μs | 2.0402 μs | 6.3850 μs |  1.55 |    0.63 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 2.065 μs | 23.381 μs | 1.2816 μs | 1.4270 μs |  0.49 |    0.31 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.707 μs | 42.394 μs | 2.3237 μs | 2.9090 μs |  0.88 |    0.56 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 3.793 μs | 43.804 μs | 2.4010 μs | 2.8050 μs |  0.90 |    0.58 |    4 |     152 B |        2.71 |
