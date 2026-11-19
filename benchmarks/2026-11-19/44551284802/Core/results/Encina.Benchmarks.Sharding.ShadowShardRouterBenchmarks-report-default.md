
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **7.014 μs** | **29.939 μs** | **1.6411 μs** |  **1.04** |    **0.32** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 4.720 μs | 24.152 μs | 1.3239 μs |  0.70 |    0.24 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 5.549 μs | 36.333 μs | 1.9915 μs |  0.83 |    0.32 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.471 μs | 22.848 μs | 1.2524 μs |  0.52 |    0.20 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.548 μs |  2.513 μs | 0.1378 μs |  0.68 |    0.16 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **6.229 μs** | **18.018 μs** | **0.9876 μs** |  **1.02** |    **0.21** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 5.809 μs | 13.603 μs | 0.7456 μs |  0.95 |    0.18 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 7.860 μs |  3.183 μs | 0.1745 μs |  1.29 |    0.20 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 6.094 μs | 12.130 μs | 0.6649 μs |  1.00 |    0.18 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 6.626 μs | 10.036 μs | 0.5501 μs |  1.08 |    0.18 |    1 |      64 B |        1.14 |
