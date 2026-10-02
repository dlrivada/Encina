
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.886 μs** | **12.250 μs** | **0.6715 μs** | **2.519 μs** |  **1.03** |    **0.28** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.888 μs | 10.945 μs | 0.5999 μs | 2.614 μs |  1.03 |    0.26 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 2.512 μs | 21.400 μs | 1.1730 μs | 2.649 μs |  0.90 |    0.40 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.888 μs | 16.232 μs | 0.8897 μs | 2.554 μs |  1.03 |    0.33 |    2 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 1.654 μs | 15.881 μs | 0.8705 μs | 1.337 μs |  0.59 |    0.29 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **1.644 μs** | **18.213 μs** | **0.9983 μs** | **1.187 μs** |  **1.23** |    **0.86** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 1.760 μs | 17.119 μs | 0.9383 μs | 1.306 μs |  1.32 |    0.85 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 2.734 μs | 26.112 μs | 1.4313 μs | 1.913 μs |  2.05 |    1.30 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.444 μs |  5.941 μs | 0.3256 μs | 2.454 μs |  1.83 |    0.78 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 2.413 μs | 35.924 μs | 1.9691 μs | 1.552 μs |  1.81 |    1.55 |    1 |      64 B |        1.14 |
