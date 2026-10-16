
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error       | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|------------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.523 μs** |   **9.0813 μs** |  **0.4978 μs** | **3.246 μs** |  **1.01** |    **0.17** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.662 μs |   2.4203 μs |  0.1327 μs | 3.612 μs |  1.05 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.381 μs |   5.6570 μs |  0.3101 μs | 4.207 μs |  1.26 |    0.16 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.427 μs |   6.2749 μs |  0.3439 μs | 3.297 μs |  0.98 |    0.14 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.131 μs |   6.2085 μs |  0.3403 μs | 4.037 μs |  1.19 |    0.16 |    1 |      64 B |        1.14 |
                                          |            |          |             |            |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.756 μs** |   **7.2506 μs** |  **0.3974 μs** | **3.606 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.182 μs |   6.6456 μs |  0.3643 μs | 4.122 μs |  1.12 |    0.13 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.930 μs |   5.8391 μs |  0.3201 μs | 4.923 μs |  1.32 |    0.14 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 9.628 μs | 201.5160 μs | 11.0458 μs | 3.316 μs |  2.58 |    2.58 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.826 μs |   0.9182 μs |  0.0503 μs | 4.819 μs |  1.29 |    0.11 |    1 |      64 B |        1.14 |
