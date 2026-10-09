
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.309 μs** |   **9.547 μs** |  **0.5233 μs** | **3.085 μs** |  **1.02** |    **0.19** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.221 μs |   9.066 μs |  0.4969 μs | 1.954 μs |  0.68 |    0.16 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.886 μs |  12.741 μs |  0.6984 μs | 2.609 μs |  0.89 |    0.22 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  4.343 μs |  14.928 μs |  0.8182 μs | 3.886 μs |  1.33 |    0.28 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  5.408 μs |  24.212 μs |  1.3271 μs | 4.657 μs |  1.66 |    0.41 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  4.049 μs |  12.333 μs |  0.6760 μs | 4.045 μs |  1.24 |    0.24 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.399 μs |  18.141 μs |  0.9944 μs | 2.955 μs |  1.04 |    0.30 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  6.864 μs |  46.795 μs |  2.5650 μs | 5.428 μs |  2.11 |    0.74 |    3 |     152 B |        2.71 |
                                  |            |           |            |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **6.370 μs** |  **19.892 μs** |  **1.0903 μs** | **5.859 μs** |  **1.02** |    **0.21** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.444 μs |   9.039 μs |  0.4954 μs | 2.193 μs |  0.39 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  3.167 μs |  10.516 μs |  0.5764 μs | 2.859 μs |  0.51 |    0.11 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  7.026 μs |  33.594 μs |  1.8414 μs | 6.175 μs |  1.12 |    0.30 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 13.894 μs | 183.570 μs | 10.0621 μs | 8.753 μs |  2.22 |    1.44 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  4.594 μs |   6.117 μs |  0.3353 μs | 4.778 μs |  0.73 |    0.11 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  4.593 μs |  10.561 μs |  0.5789 μs | 4.426 μs |  0.73 |    0.13 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  7.064 μs |  18.085 μs |  0.9913 μs | 7.261 μs |  1.13 |    0.21 |    4 |     152 B |        2.71 |
