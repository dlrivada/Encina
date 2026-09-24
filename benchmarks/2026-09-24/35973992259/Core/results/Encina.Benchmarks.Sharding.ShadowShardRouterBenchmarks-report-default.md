
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.126 μs** |  **5.080 μs** | **0.2784 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.080 μs |  2.961 μs | 0.1623 μs |  0.99 |    0.09 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.749 μs |  6.464 μs | 0.3543 μs |  1.53 |    0.15 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.594 μs |  8.838 μs | 0.4844 μs |  1.16 |    0.16 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.433 μs |  7.069 μs | 0.3875 μs |  1.10 |    0.14 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.634 μs** |  **4.667 μs** | **0.2558 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 5.571 μs | 36.531 μs | 2.0024 μs |  1.54 |    0.49 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.053 μs |  4.984 μs | 0.2732 μs |  1.40 |    0.10 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.580 μs |  3.105 μs | 0.1702 μs |  0.99 |    0.07 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.791 μs |  3.275 μs | 0.1795 μs |  1.05 |    0.08 |    1 |      64 B |        1.14 |
