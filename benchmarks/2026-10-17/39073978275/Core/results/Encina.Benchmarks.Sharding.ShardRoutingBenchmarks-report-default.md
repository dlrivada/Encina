
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.410 μs** | **22.779 μs** | **1.2486 μs** | **3.083 μs** |  **1.09** |    **0.48** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 3.433 μs | 16.695 μs | 0.9151 μs | 3.139 μs |  1.10 |    0.41 |    2 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.272 μs | 19.435 μs | 1.0653 μs | 1.872 μs |  0.72 |    0.37 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 5.351 μs | 32.605 μs | 1.7872 μs | 4.405 μs |  1.71 |    0.72 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.798 μs | 33.251 μs | 1.8226 μs | 5.925 μs |  2.17 |    0.82 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.081 μs | 26.944 μs | 1.4769 μs | 2.487 μs |  0.98 |    0.51 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.075 μs | 27.756 μs | 1.5214 μs | 2.389 μs |  0.98 |    0.52 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.498 μs | 47.359 μs | 2.5959 μs | 4.529 μs |  1.75 |    0.90 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.888 μs** | **12.447 μs** | **0.6822 μs** | **3.666 μs** |  **1.02** |    **0.21** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 4.279 μs |  9.602 μs | 0.5263 μs | 4.556 μs |  1.12 |    0.20 |    2 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.234 μs |  9.813 μs | 0.5379 μs | 2.975 μs |  0.85 |    0.17 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 6.402 μs | 15.557 μs | 0.8527 μs | 6.169 μs |  1.68 |    0.31 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.220 μs | 22.415 μs | 1.2287 μs | 7.883 μs |  2.16 |    0.42 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.993 μs | 12.771 μs | 0.7000 μs | 3.906 μs |  1.05 |    0.22 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.383 μs | 40.917 μs | 2.2428 μs | 4.654 μs |  1.41 |    0.55 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.508 μs | 19.387 μs | 1.0626 μs | 6.128 μs |  1.71 |    0.34 |    3 |     152 B |        2.71 |
