
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.339 μs** |  **22.457 μs** | **1.2310 μs** | **2.794 μs** |  **1.08** |    **0.46** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.468 μs |   6.185 μs | 0.3390 μs | 2.524 μs |  0.80 |    0.24 |    3 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.392 μs |   3.828 μs | 0.2098 μs | 2.319 μs |  0.78 |    0.22 |    3 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 2.136 μs |  30.776 μs | 1.6870 μs | 1.401 μs |  0.69 |    0.52 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 3.315 μs |  35.125 μs | 1.9253 μs | 2.244 μs |  1.07 |    0.63 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 1.639 μs |  17.585 μs | 0.9639 μs | 1.202 μs |  0.53 |    0.31 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 1.899 μs |  31.628 μs | 1.7337 μs | 1.121 μs |  0.62 |    0.53 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 2.985 μs |  42.773 μs | 2.3446 μs | 1.954 μs |  0.97 |    0.73 |    4 |     152 B |        2.71 |
                                  |            |          |            |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.050 μs** |   **7.562 μs** | **0.4145 μs** | **4.116 μs** |  **1.01** |    **0.13** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.648 μs |   8.324 μs | 0.4563 μs | 2.728 μs |  0.66 |    0.12 |    2 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.936 μs |  13.101 μs | 0.7181 μs | 2.599 μs |  0.73 |    0.17 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 2.489 μs |  32.118 μs | 1.7605 μs | 1.728 μs |  0.62 |    0.38 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 4.477 μs |  20.006 μs | 1.0966 μs | 3.936 μs |  1.11 |    0.26 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.300 μs |  27.415 μs | 1.5027 μs | 3.555 μs |  1.07 |    0.34 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.165 μs |  38.500 μs | 2.1103 μs | 2.454 μs |  0.79 |    0.46 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 9.020 μs | 131.763 μs | 7.2224 μs | 5.519 μs |  2.24 |    1.57 |    4 |     152 B |        2.71 |
