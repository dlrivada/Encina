
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.890 μs** | **30.668 μs** | **1.6810 μs** | **2.935 μs** |  **1.11** |    **0.55** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.115 μs |  3.646 μs | 0.1998 μs | 3.005 μs |  0.89 |    0.27 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.993 μs |  4.303 μs | 0.2359 μs | 4.970 μs |  1.43 |    0.43 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.275 μs |  2.960 μs | 0.1622 μs | 3.191 μs |  0.94 |    0.28 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.141 μs |  2.849 μs | 0.1562 μs | 3.051 μs |  0.90 |    0.27 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.419 μs** |  **1.959 μs** | **0.1074 μs** | **3.396 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.627 μs |  5.941 μs | 0.3256 μs | 3.616 μs |  1.06 |    0.09 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.062 μs |  8.225 μs | 0.4509 μs | 4.848 μs |  1.48 |    0.12 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.353 μs |  1.555 μs | 0.0852 μs | 3.387 μs |  0.98 |    0.03 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.295 μs |  7.410 μs | 0.4062 μs | 4.118 μs |  1.26 |    0.11 |    1 |      64 B |        1.14 |
