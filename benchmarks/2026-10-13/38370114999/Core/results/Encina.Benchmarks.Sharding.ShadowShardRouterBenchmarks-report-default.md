
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.667 μs** |  **9.319 μs** | **0.5108 μs** | **2.413 μs** |  **1.02** |    **0.23** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.594 μs |  6.646 μs | 0.3643 μs | 2.784 μs |  0.99 |    0.19 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.355 μs | 16.083 μs | 0.8816 μs | 4.112 μs |  1.67 |    0.39 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.701 μs |  4.227 μs | 0.2317 μs | 2.674 μs |  1.04 |    0.17 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.894 μs | 15.786 μs | 0.8653 μs | 2.429 μs |  1.11 |    0.33 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.832 μs** | **15.699 μs** | **0.8605 μs** | **3.624 μs** |  **1.03** |    **0.28** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.926 μs | 12.791 μs | 0.7011 μs | 3.626 μs |  1.06 |    0.26 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.537 μs | 11.013 μs | 0.6036 μs | 4.327 μs |  1.22 |    0.27 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.946 μs | 24.391 μs | 1.3370 μs | 3.345 μs |  1.06 |    0.37 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.569 μs | 29.640 μs | 1.6247 μs | 3.641 μs |  1.23 |    0.45 |    2 |      64 B |        1.14 |
