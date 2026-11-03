
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.595 μs** | **12.2666 μs** | **0.6724 μs** |  **1.02** |    **0.23** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.162 μs |  2.4354 μs | 0.1335 μs |  0.61 |    0.10 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.545 μs |  2.3717 μs | 0.1300 μs |  0.72 |    0.11 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.234 μs |  9.9449 μs | 0.5451 μs |  1.20 |    0.22 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.207 μs |  6.9136 μs | 0.3790 μs |  1.76 |    0.28 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.289 μs |  2.4939 μs | 0.1367 μs |  0.93 |    0.14 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.351 μs |  1.7632 μs | 0.0966 μs |  0.95 |    0.14 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.430 μs | 10.6060 μs | 0.5814 μs |  1.54 |    0.27 |    4 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.460 μs** |  **3.5998 μs** | **0.1973 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.705 μs |  7.7298 μs | 0.4237 μs |  0.78 |    0.11 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.713 μs |  2.1380 μs | 0.1172 μs |  0.79 |    0.05 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.345 μs |  5.9752 μs | 0.3275 μs |  1.26 |    0.10 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.439 μs | 15.4393 μs | 0.8463 μs |  2.44 |    0.24 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.460 μs |  1.8455 μs | 0.1012 μs |  1.00 |    0.05 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.630 μs | 16.8237 μs | 0.9222 μs |  1.63 |    0.24 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.108 μs |  0.9043 μs | 0.0496 μs |  1.77 |    0.09 |    4 |     152 B |        2.71 |
