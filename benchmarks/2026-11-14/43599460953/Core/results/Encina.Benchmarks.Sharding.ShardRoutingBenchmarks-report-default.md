
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **4.295 μs** | **15.901 μs** | **0.8716 μs** |  **1.03** |    **0.26** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 3.527 μs |  6.636 μs | 0.3637 μs |  0.84 |    0.17 |    2 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.589 μs |  2.007 μs | 0.1100 μs |  0.62 |    0.11 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.294 μs | 16.035 μs | 0.8790 μs |  1.03 |    0.26 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.896 μs |  4.743 μs | 0.2600 μs |  1.41 |    0.26 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.896 μs |  8.067 μs | 0.4422 μs |  0.93 |    0.19 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.472 μs | 15.859 μs | 0.8693 μs |  1.07 |    0.27 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.658 μs |  3.837 μs | 0.2103 μs |  1.36 |    0.25 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.834 μs** |  **7.898 μs** | **0.4329 μs** |  **1.01** |    **0.14** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.134 μs |  1.026 μs | 0.0562 μs |  0.56 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.896 μs |  4.217 μs | 0.2311 μs |  0.76 |    0.09 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.851 μs | 17.429 μs | 0.9553 μs |  1.28 |    0.25 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.196 μs | 19.249 μs | 1.0551 μs |  2.16 |    0.32 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.486 μs |  1.973 μs | 0.1082 μs |  0.92 |    0.09 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.876 μs |  7.465 μs | 0.4092 μs |  1.28 |    0.16 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.694 μs | 15.108 μs | 0.8281 μs |  1.76 |    0.25 |    5 |     152 B |        2.71 |
