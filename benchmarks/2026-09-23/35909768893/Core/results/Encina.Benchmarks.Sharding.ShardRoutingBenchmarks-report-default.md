
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error      | StdDev   | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|-----------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **5.069 μs** |  **35.930 μs** | **1.969 μs** | **4.289 μs** |  **1.09** |    **0.49** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  3.603 μs |  20.022 μs | 1.098 μs | 3.132 μs |  0.78 |    0.31 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  4.670 μs |  21.312 μs | 1.168 μs | 4.468 μs |  1.01 |    0.37 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 11.223 μs | 156.223 μs | 8.563 μs | 9.134 μs |  2.42 |    1.80 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 10.090 μs |  41.442 μs | 2.272 μs | 9.232 μs |  2.18 |    0.77 |    2 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  5.447 μs |  31.718 μs | 1.739 μs | 4.753 μs |  1.17 |    0.48 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  4.443 μs |  40.189 μs | 2.203 μs | 3.388 μs |  0.96 |    0.51 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  6.605 μs |  58.663 μs | 3.215 μs | 5.473 μs |  1.42 |    0.75 |    1 |     152 B |        2.71 |
                                  |            |           |            |          |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **4.800 μs** |  **28.497 μs** | **1.562 μs** | **4.345 μs** |  **1.07** |    **0.41** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.874 μs |  19.324 μs | 1.059 μs | 2.355 μs |  0.64 |    0.27 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  3.538 μs |  21.958 μs | 1.204 μs | 2.874 μs |  0.79 |    0.31 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  6.160 μs |  30.016 μs | 1.645 μs | 5.777 μs |  1.37 |    0.48 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 10.198 μs |  30.639 μs | 1.679 μs | 9.350 μs |  2.27 |    0.67 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  5.103 μs |  22.878 μs | 1.254 μs | 4.408 μs |  1.14 |    0.38 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  7.738 μs |  37.311 μs | 2.045 μs | 6.834 μs |  1.72 |    0.60 |    5 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  7.738 μs |  37.550 μs | 2.058 μs | 7.053 μs |  1.72 |    0.60 |    5 |     152 B |        2.71 |
