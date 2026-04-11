
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.215 μs** | **0.2609 μs** | **0.3742 μs** | **3.091 μs** |  **1.01** |    **0.16** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.909 μs | 0.0312 μs | 0.0426 μs | 2.895 μs |  0.92 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.090 μs | 0.0681 μs | 0.0954 μs | 4.077 μs |  1.29 |    0.15 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.090 μs | 0.0739 μs | 0.1036 μs | 3.066 μs |  0.97 |    0.11 |    2 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.223 μs | 0.0275 μs | 0.0376 μs | 3.221 μs |  1.02 |    0.11 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.339 μs** | **0.1699 μs** | **0.2543 μs** | **3.362 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.632 μs | 0.2717 μs | 0.3983 μs | 3.803 μs |  1.09 |    0.14 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.905 μs | 0.3396 μs | 0.4649 μs | 4.829 μs |  1.48 |    0.18 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.252 μs | 0.0594 μs | 0.0833 μs | 3.246 μs |  0.98 |    0.08 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.092 μs | 0.2243 μs | 0.3287 μs | 3.907 μs |  1.23 |    0.13 |    1 |      64 B |        1.14 |
