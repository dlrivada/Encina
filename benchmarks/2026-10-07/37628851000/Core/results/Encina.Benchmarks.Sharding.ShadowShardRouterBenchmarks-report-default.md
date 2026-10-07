
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.095 μs** |  **4.858 μs** | **0.2663 μs** |  **1.00** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.140 μs | 12.989 μs | 0.7119 μs |  1.02 |    0.21 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.322 μs |  5.400 μs | 0.2960 μs |  1.40 |    0.13 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.302 μs |  5.080 μs | 0.2785 μs |  1.07 |    0.11 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.897 μs | 10.751 μs | 0.5893 μs |  1.27 |    0.19 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.911 μs** |  **7.300 μs** | **0.4002 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.974 μs |  3.854 μs | 0.2113 μs |  1.02 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.442 μs |  6.417 μs | 0.3517 μs |  1.40 |    0.14 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.564 μs | 10.292 μs | 0.5641 μs |  1.17 |    0.16 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.074 μs |  7.652 μs | 0.4194 μs |  1.05 |    0.13 |    1 |      64 B |        1.14 |
