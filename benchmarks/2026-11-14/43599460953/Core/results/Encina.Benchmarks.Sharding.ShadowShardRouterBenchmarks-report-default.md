
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.113 μs** | **0.1005 μs** | **0.0055 μs** |  **1.00** |    **0.00** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.933 μs | 2.6206 μs | 0.1436 μs |  0.94 |    0.04 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.217 μs | 3.3297 μs | 0.1825 μs |  1.35 |    0.05 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.189 μs | 3.7549 μs | 0.2058 μs |  1.02 |    0.06 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.420 μs | 3.0424 μs | 0.1668 μs |  1.10 |    0.05 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.316 μs** | **4.3230 μs** | **0.2370 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.191 μs | 1.6016 μs | 0.0878 μs |  1.27 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.086 μs | 8.9959 μs | 0.4931 μs |  1.54 |    0.16 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.345 μs | 2.7352 μs | 0.1499 μs |  1.01 |    0.07 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.741 μs | 2.7542 μs | 0.1510 μs |  1.13 |    0.08 |    1 |      64 B |        1.14 |
