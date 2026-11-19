
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.637 μs** |  **6.140 μs** | **0.3365 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.454 μs |  5.400 μs | 0.2960 μs |  0.94 |    0.14 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.318 μs | 10.750 μs | 0.5892 μs |  1.27 |    0.24 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.674 μs |  4.837 μs | 0.2651 μs |  1.02 |    0.14 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.901 μs | 15.244 μs | 0.8356 μs |  1.11 |    0.30 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.926 μs** |  **6.546 μs** | **0.3588 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.995 μs |  4.118 μs | 0.2257 μs |  1.03 |    0.13 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.443 μs | 15.617 μs | 0.8560 μs |  1.53 |    0.30 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.898 μs |  4.313 μs | 0.2364 μs |  1.00 |    0.13 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.462 μs | 11.003 μs | 0.6031 μs |  1.19 |    0.22 |    1 |      64 B |        1.14 |
