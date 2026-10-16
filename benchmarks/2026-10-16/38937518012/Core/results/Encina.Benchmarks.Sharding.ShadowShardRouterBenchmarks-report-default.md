
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.661 μs** |  **5.301 μs** | **0.2906 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.603 μs |  4.108 μs | 0.2252 μs |  0.99 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.233 μs |  9.193 μs | 0.5039 μs |  1.22 |    0.20 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.445 μs |  6.318 μs | 0.3463 μs |  0.93 |    0.14 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.644 μs | 21.500 μs | 1.1785 μs |  1.38 |    0.41 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.687 μs** |  **5.185 μs** | **0.2842 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.098 μs |  9.088 μs | 0.4982 μs |  1.16 |    0.19 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 3.855 μs |  5.404 μs | 0.2962 μs |  1.44 |    0.16 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.260 μs | 30.080 μs | 1.6488 μs |  1.60 |    0.55 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.165 μs |  7.218 μs | 0.3957 μs |  1.19 |    0.16 |    1 |      64 B |        1.14 |
