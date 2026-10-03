
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.683 μs** |  **9.965 μs** | **0.5462 μs** |  **1.03** |    **0.25** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.731 μs |  7.672 μs | 0.4205 μs |  1.04 |    0.22 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.487 μs |  3.265 μs | 0.1790 μs |  1.33 |    0.23 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.545 μs |  9.870 μs | 0.5410 μs |  0.97 |    0.24 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.693 μs |  8.866 μs | 0.4860 μs |  1.03 |    0.23 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.753 μs** |  **6.834 μs** | **0.3746 μs** |  **1.01** |    **0.16** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.264 μs | 21.713 μs | 1.1902 μs |  1.20 |    0.40 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.720 μs | 20.370 μs | 1.1166 μs |  1.73 |    0.40 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.046 μs | 16.267 μs | 0.8917 μs |  1.49 |    0.33 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.559 μs | 38.889 μs | 2.1316 μs |  2.04 |    0.72 |    2 |      64 B |        1.14 |
