
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.727 μs** | **5.1443 μs** | **0.2820 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.659 μs | 2.8496 μs | 0.1562 μs |  0.99 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.169 μs | 1.5191 μs | 0.0833 μs |  1.12 |    0.08 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.293 μs | 1.6460 μs | 0.0902 μs |  0.89 |    0.06 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.489 μs | 1.9782 μs | 0.1084 μs |  0.94 |    0.07 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.430 μs** | **0.6407 μs** | **0.0351 μs** |  **1.00** |    **0.01** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.539 μs | 2.4499 μs | 0.1343 μs |  1.03 |    0.04 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.052 μs | 6.4180 μs | 0.3518 μs |  1.47 |    0.09 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.334 μs | 2.1190 μs | 0.1161 μs |  1.26 |    0.03 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.967 μs | 0.5473 μs | 0.0300 μs |  1.16 |    0.01 |    1 |      64 B |        1.14 |
