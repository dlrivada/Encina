
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.153 μs** | **14.233 μs** | **0.7802 μs** | **2.770 μs** |  **1.04** |    **0.30** |    **3** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.479 μs | 22.107 μs | 1.2117 μs | 1.061 μs |  0.49 |    0.36 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.138 μs | 32.184 μs | 1.7641 μs | 3.224 μs |  1.03 |    0.55 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.552 μs | 22.460 μs | 1.2311 μs | 1.051 μs |  0.51 |    0.37 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.814 μs | 18.723 μs | 1.0263 μs | 2.614 μs |  0.93 |    0.34 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.116 μs** | **17.571 μs** | **0.9631 μs** | **1.641 μs** |  **1.12** |    **0.58** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.108 μs | 19.296 μs | 1.0577 μs | 1.637 μs |  1.12 |    0.62 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.125 μs | 19.365 μs | 1.0615 μs | 4.537 μs |  2.72 |    1.00 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.083 μs | 22.592 μs | 1.2383 μs | 1.502 μs |  1.11 |    0.69 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 2.173 μs | 24.863 μs | 1.3628 μs | 1.672 μs |  1.15 |    0.75 |    1 |      64 B |        1.14 |
