
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error      | StdDev     | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|-----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **2.194 μs** |  **24.253 μs** |  **1.3294 μs** |  **2.050 μs** |  **1.34** |    **1.11** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  3.223 μs |  25.313 μs |  1.3875 μs |  3.267 μs |  1.96 |    1.40 |    2 |      48 B |        0.86 |
 'Directory routing'              | 3          |  3.554 μs |  13.930 μs |  0.7635 μs |  3.188 μs |  2.16 |    1.33 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  4.912 μs |  45.887 μs |  2.5152 μs |  6.080 μs |  2.99 |    2.29 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  5.856 μs |  37.105 μs |  2.0338 μs |  5.096 μs |  3.57 |    2.39 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  2.775 μs |  24.027 μs |  1.3170 μs |  2.357 μs |  1.69 |    1.25 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  2.664 μs |  23.458 μs |  1.2858 μs |  2.065 μs |  1.62 |    1.21 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  7.368 μs |  31.392 μs |  1.7207 μs |  6.479 μs |  4.49 |    2.78 |    5 |     152 B |        2.71 |
                                  |            |           |            |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **5.534 μs** |   **4.568 μs** |  **0.2504 μs** |  **5.441 μs** |  **1.00** |    **0.05** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  3.993 μs |  23.289 μs |  1.2766 μs |  3.285 μs |  0.72 |    0.20 |    2 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.977 μs |   9.812 μs |  0.5378 μs |  2.686 μs |  0.54 |    0.09 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  5.311 μs |  27.137 μs |  1.4875 μs |  5.923 μs |  0.96 |    0.24 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 10.758 μs |  15.860 μs |  0.8694 μs | 10.642 μs |  1.95 |    0.16 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  6.225 μs |  21.137 μs |  1.1586 μs |  6.463 μs |  1.13 |    0.19 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  7.237 μs |  29.472 μs |  1.6154 μs |  6.781 μs |  1.31 |    0.26 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 15.411 μs | 218.634 μs | 11.9841 μs |  8.828 μs |  2.79 |    1.88 |    5 |     152 B |        2.71 |
