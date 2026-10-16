
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.405 μs** | **3.381 μs** | **0.1853 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.460 μs | 4.197 μs | 0.2301 μs |  1.03 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.270 μs | 5.630 μs | 0.3086 μs |  1.36 |    0.14 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.547 μs | 3.346 μs | 0.1834 μs |  1.06 |    0.10 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.601 μs | 6.401 μs | 0.3509 μs |  1.09 |    0.14 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.968 μs** | **6.302 μs** | **0.3454 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.802 μs | 3.281 μs | 0.1799 μs |  0.95 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 3.956 μs | 4.866 μs | 0.2667 μs |  1.34 |    0.15 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.572 μs | 4.067 μs | 0.2229 μs |  0.87 |    0.11 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.404 μs | 9.368 μs | 0.5135 μs |  1.16 |    0.19 |    1 |      64 B |        1.14 |
