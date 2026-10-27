
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.894 μs** |  **1.594 μs** | **0.0874 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.442 μs |  6.293 μs | 0.3449 μs |  0.88 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.579 μs |  9.915 μs | 0.5435 μs |  1.18 |    0.12 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.748 μs | 12.673 μs | 0.6946 μs |  0.96 |    0.16 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.476 μs |  1.425 μs | 0.0781 μs |  0.89 |    0.02 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.592 μs** |  **2.743 μs** | **0.1503 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.445 μs |  3.253 μs | 0.1783 μs |  0.96 |    0.06 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.546 μs | 10.020 μs | 0.5493 μs |  1.55 |    0.14 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.650 μs |  5.806 μs | 0.3182 μs |  1.02 |    0.09 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.013 μs |  2.533 μs | 0.1388 μs |  1.12 |    0.05 |    1 |      64 B |        1.14 |
