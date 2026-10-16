
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.529 μs** |  **1.981 μs** | **0.1086 μs** | **2.5590 μs** |  **1.00** |    **0.05** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.599 μs | 17.941 μs | 0.9834 μs | 1.3120 μs |  0.63 |    0.34 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 2.823 μs | 34.958 μs | 1.9162 μs | 2.9500 μs |  1.12 |    0.66 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.396 μs | 22.389 μs | 1.2272 μs | 0.8920 μs |  0.55 |    0.42 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 1.476 μs | 22.517 μs | 1.2342 μs | 0.8810 μs |  0.58 |    0.42 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **1.731 μs** | **20.741 μs** | **1.1369 μs** | **1.1670 μs** |  **1.27** |    **0.94** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 1.978 μs | 19.942 μs | 1.0931 μs | 1.3670 μs |  1.45 |    0.97 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 2.801 μs | 24.602 μs | 1.3485 μs | 2.0940 μs |  2.05 |    1.27 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 1.819 μs | 17.156 μs | 0.9404 μs | 1.5520 μs |  1.33 |    0.86 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 2.010 μs | 20.531 μs | 1.1254 μs | 1.4520 μs |  1.47 |    0.99 |    2 |      64 B |        1.14 |
