
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.463 μs** | **0.6421 μs** | **0.9002 μs** |  **1.07** |    **0.39** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 4.246 μs | 0.3065 μs | 0.4395 μs |  1.31 |    0.36 |    3 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.630 μs | 0.2567 μs | 0.3514 μs |  1.12 |    0.31 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.596 μs | 0.2114 μs | 0.2894 μs |  0.80 |    0.23 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.049 μs | 0.1586 μs | 0.2117 μs |  0.94 |    0.25 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.726 μs** | **0.1420 μs** | **0.2036 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.807 μs | 0.1194 μs | 0.1712 μs |  1.02 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.832 μs | 0.2515 μs | 0.3526 μs |  1.30 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.721 μs | 0.1734 μs | 0.2542 μs |  1.00 |    0.08 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.589 μs | 0.1084 μs | 0.1555 μs |  1.24 |    0.08 |    2 |      64 B |        1.14 |
