
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.152 μs** |  **3.1125 μs** | **0.1706 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.735 μs |  6.9795 μs | 0.3826 μs |  0.87 |    0.11 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.545 μs |  1.3257 μs | 0.0727 μs |  0.81 |    0.04 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.358 μs | 12.9255 μs | 0.7085 μs |  1.39 |    0.21 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.201 μs |  5.9201 μs | 0.3245 μs |  1.97 |    0.13 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.183 μs |  2.5225 μs | 0.1383 μs |  1.01 |    0.06 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.375 μs |  3.1086 μs | 0.1704 μs |  1.07 |    0.07 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.070 μs |  6.0914 μs | 0.3339 μs |  1.93 |    0.13 |    3 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.514 μs** |  **4.1154 μs** | **0.2256 μs** |  **1.00** |    **0.08** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.177 μs |  0.9960 μs | 0.0546 μs |  0.62 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.581 μs |  2.9302 μs | 0.1606 μs |  0.74 |    0.06 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.823 μs |  3.8507 μs | 0.2111 μs |  1.09 |    0.08 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.798 μs |  5.9853 μs | 0.3281 μs |  2.23 |    0.15 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.027 μs |  7.0282 μs | 0.3852 μs |  1.15 |    0.11 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.954 μs |  8.3913 μs | 0.4600 μs |  1.13 |    0.13 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.541 μs |  7.4463 μs | 0.4082 μs |  1.58 |    0.13 |    3 |     152 B |        2.71 |
