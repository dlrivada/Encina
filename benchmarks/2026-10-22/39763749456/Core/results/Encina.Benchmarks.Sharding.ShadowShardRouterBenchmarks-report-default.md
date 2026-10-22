
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.152 μs** | **1.695 μs** | **0.0929 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.168 μs | 3.840 μs | 0.2105 μs |  1.01 |    0.06 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.983 μs | 6.932 μs | 0.3800 μs |  1.58 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.720 μs | 7.016 μs | 0.3845 μs |  1.18 |    0.11 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.272 μs | 3.202 μs | 0.1755 μs |  1.04 |    0.06 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.406 μs** | **1.430 μs** | **0.0784 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.456 μs | 3.635 μs | 0.1992 μs |  1.02 |    0.05 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.866 μs | 4.907 μs | 0.2690 μs |  1.43 |    0.07 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.456 μs | 2.850 μs | 0.1562 μs |  1.02 |    0.04 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.807 μs | 4.045 μs | 0.2217 μs |  1.12 |    0.06 |    1 |      64 B |        1.14 |
