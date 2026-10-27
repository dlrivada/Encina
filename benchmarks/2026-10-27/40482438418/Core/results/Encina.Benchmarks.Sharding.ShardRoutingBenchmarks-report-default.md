
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **1.654 μs** | **26.320 μs** | **1.4427 μs** | **0.9860 μs** |  **1.55** |    **1.58** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.009 μs | 14.957 μs | 0.8198 μs | 0.7055 μs |  0.95 |    0.92 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.330 μs | 17.691 μs | 0.9697 μs | 0.9160 μs |  1.25 |    1.13 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 2.272 μs | 32.555 μs | 1.7845 μs | 1.4270 μs |  2.13 |    2.02 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 2.621 μs | 26.842 μs | 1.4713 μs | 2.1240 μs |  2.46 |    1.92 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 1.663 μs | 24.888 μs | 1.3642 μs | 1.2520 μs |  1.56 |    1.52 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.009 μs | 33.250 μs | 1.8226 μs | 1.1215 μs |  1.89 |    1.97 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 3.096 μs | 43.594 μs | 2.3895 μs | 2.0275 μs |  2.91 |    2.72 |    5 |     152 B |        2.71 |
                                  |            |          |           |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **1.946 μs** | **22.467 μs** | **1.2315 μs** | **1.3115 μs** |  **1.25** |    **0.89** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.430 μs |  3.589 μs | 0.1967 μs | 2.3530 μs |  1.56 |    0.64 |    2 |      48 B |        0.86 |
 'Directory routing'              | 50         | 1.781 μs | 17.929 μs | 0.9827 μs | 1.3870 μs |  1.14 |    0.75 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 2.006 μs | 27.167 μs | 1.4891 μs | 1.1820 μs |  1.28 |    1.03 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 4.787 μs | 30.896 μs | 1.6935 μs | 3.8960 μs |  3.06 |    1.60 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 2.539 μs | 31.658 μs | 1.7353 μs | 2.0280 μs |  1.63 |    1.22 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 2.838 μs | 31.937 μs | 1.7506 μs | 1.8930 μs |  1.82 |    1.28 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 3.396 μs | 44.706 μs | 2.4505 μs | 2.3840 μs |  2.17 |    1.70 |    4 |     152 B |        2.71 |
