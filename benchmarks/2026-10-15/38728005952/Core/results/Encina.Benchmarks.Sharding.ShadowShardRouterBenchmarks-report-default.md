
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.199 μs** |  **2.490 μs** | **0.1365 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.592 μs |  4.706 μs | 0.2579 μs |  1.12 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.072 μs |  4.041 μs | 0.2215 μs |  1.27 |    0.08 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.143 μs |  4.079 μs | 0.2236 μs |  0.98 |    0.07 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.355 μs |  4.814 μs | 0.2639 μs |  1.05 |    0.08 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.514 μs** | **10.352 μs** | **0.5674 μs** |  **1.02** |    **0.19** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.353 μs |  3.396 μs | 0.1862 μs |  0.97 |    0.13 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.559 μs | 13.474 μs | 0.7386 μs |  1.90 |    0.31 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.546 μs |  1.622 μs | 0.0889 μs |  1.03 |    0.13 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.047 μs |  7.714 μs | 0.4228 μs |  1.46 |    0.22 |    2 |      64 B |        1.14 |
