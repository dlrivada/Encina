
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.339 μs** |  **6.322 μs** | **0.3466 μs** |  **1.01** |    **0.13** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.087 μs |  3.095 μs | 0.1696 μs |  0.63 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.731 μs |  7.293 μs | 0.3998 μs |  0.82 |    0.13 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.385 μs |  9.141 μs | 0.5011 μs |  1.32 |    0.18 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.013 μs |  8.807 μs | 0.4828 μs |  1.51 |    0.19 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.222 μs |  4.980 μs | 0.2730 μs |  0.97 |    0.11 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.459 μs |  6.609 μs | 0.3623 μs |  1.04 |    0.13 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.884 μs |  7.871 μs | 0.4314 μs |  1.78 |    0.19 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.121 μs** |  **1.104 μs** | **0.0605 μs** |  **1.00** |    **0.02** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.300 μs |  1.640 μs | 0.0899 μs |  0.56 |    0.02 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.814 μs |  3.389 μs | 0.1858 μs |  0.68 |    0.04 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.551 μs | 10.759 μs | 0.5898 μs |  1.10 |    0.12 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.036 μs |  5.165 μs | 0.2831 μs |  1.71 |    0.06 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.363 μs |  6.962 μs | 0.3816 μs |  0.82 |    0.08 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.173 μs |  7.507 μs | 0.4115 μs |  1.01 |    0.09 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.402 μs | 13.888 μs | 0.7612 μs |  1.55 |    0.16 |    5 |     152 B |        2.71 |
