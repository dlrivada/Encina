
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.107 μs** | **7.0629 μs** | **0.3871 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.960 μs | 2.1127 μs | 0.1158 μs |  0.96 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.132 μs | 2.3562 μs | 0.1292 μs |  1.34 |    0.14 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.079 μs | 3.2941 μs | 0.1806 μs |  1.00 |    0.11 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.383 μs | 2.2956 μs | 0.1258 μs |  1.10 |    0.12 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.493 μs** | **3.9718 μs** | **0.2177 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.314 μs | 1.3152 μs | 0.0721 μs |  1.24 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.757 μs | 2.0096 μs | 0.1102 μs |  1.37 |    0.08 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.416 μs | 0.3265 μs | 0.0179 μs |  0.98 |    0.05 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.859 μs | 2.4538 μs | 0.1345 μs |  1.11 |    0.07 |    1 |      64 B |        1.14 |
