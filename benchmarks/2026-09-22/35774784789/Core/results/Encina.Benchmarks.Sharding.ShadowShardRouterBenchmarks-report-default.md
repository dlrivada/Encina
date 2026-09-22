
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.004 μs** | **3.975 μs** | **0.2179 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.061 μs | 2.435 μs | 0.1335 μs |  1.02 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.428 μs | 3.408 μs | 0.1868 μs |  1.48 |    0.10 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.374 μs | 2.010 μs | 0.1102 μs |  1.13 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.513 μs | 4.698 μs | 0.2575 μs |  1.17 |    0.10 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.443 μs** | **2.108 μs** | **0.1155 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.419 μs | 1.117 μs | 0.0612 μs |  0.99 |    0.03 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.799 μs | 1.804 μs | 0.0989 μs |  1.39 |    0.05 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.431 μs | 1.629 μs | 0.0893 μs |  1.00 |    0.04 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.810 μs | 3.398 μs | 0.1862 μs |  1.11 |    0.06 |    1 |      64 B |        1.14 |
