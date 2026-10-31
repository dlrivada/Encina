
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.337 μs** |  **7.371 μs** | **0.4040 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.634 μs |  5.371 μs | 0.2944 μs |  1.10 |    0.13 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.173 μs |  5.541 μs | 0.3037 μs |  1.26 |    0.15 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.264 μs |  2.658 μs | 0.1457 μs |  0.99 |    0.10 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.893 μs |  2.889 μs | 0.1583 μs |  1.18 |    0.12 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.741 μs** | **14.034 μs** | **0.7692 μs** |  **1.03** |    **0.25** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.456 μs |  2.650 μs | 0.1453 μs |  0.95 |    0.17 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.641 μs |  4.449 μs | 0.2438 μs |  1.55 |    0.27 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.215 μs |  6.031 μs | 0.3306 μs |  1.16 |    0.21 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.020 μs |  1.594 μs | 0.0874 μs |  1.10 |    0.19 |    1 |      64 B |        1.14 |
