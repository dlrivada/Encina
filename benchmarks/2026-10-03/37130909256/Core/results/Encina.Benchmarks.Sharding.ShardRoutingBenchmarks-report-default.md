
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.638 μs** | **28.724 μs** | **1.5745 μs** | **3.013 μs** |  **1.12** |    **0.56** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.041 μs | 20.581 μs | 1.1281 μs | 1.536 μs |  0.63 |    0.37 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 4.697 μs | 27.197 μs | 1.4907 μs | 4.199 μs |  1.44 |    0.62 |    3 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 5.802 μs | 39.026 μs | 2.1391 μs | 4.947 μs |  1.78 |    0.82 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 7.047 μs | 37.230 μs | 2.0407 μs | 5.888 μs |  2.16 |    0.89 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.352 μs | 36.583 μs | 2.0053 μs | 2.390 μs |  1.03 |    0.64 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.541 μs | 39.177 μs | 2.1474 μs | 2.553 μs |  1.09 |    0.69 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.757 μs | 42.177 μs | 2.3119 μs | 5.919 μs |  2.07 |    0.92 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.441 μs** | **26.307 μs** | **1.4420 μs** | **3.675 μs** |  **1.06** |    **0.40** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.970 μs | 12.236 μs | 0.6707 μs | 2.712 μs |  0.71 |    0.22 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.518 μs | 18.912 μs | 1.0367 μs | 3.167 μs |  0.84 |    0.30 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 5.607 μs | 21.948 μs | 1.2031 μs | 5.230 μs |  1.34 |    0.41 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.980 μs | 46.701 μs | 2.5598 μs | 8.803 μs |  2.39 |    0.79 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.499 μs | 26.257 μs | 1.4392 μs | 4.088 μs |  1.08 |    0.40 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.129 μs | 25.979 μs | 1.4240 μs | 4.602 μs |  1.23 |    0.42 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 8.085 μs | 48.654 μs | 2.6669 μs | 6.873 μs |  1.94 |    0.73 |    3 |     152 B |        2.71 |
