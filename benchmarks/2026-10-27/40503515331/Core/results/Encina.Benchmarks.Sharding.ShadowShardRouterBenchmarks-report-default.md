
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.696 μs** |  **7.951 μs** | **0.4358 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.282 μs |  3.593 μs | 0.1969 μs |  0.90 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.737 μs |  8.205 μs | 0.4497 μs |  1.29 |    0.18 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.566 μs |  7.354 μs | 0.4031 μs |  0.97 |    0.14 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.704 μs |  5.883 μs | 0.3225 μs |  1.01 |    0.13 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.749 μs** | **12.960 μs** | **0.7104 μs** |  **1.01** |    **0.18** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.008 μs | 15.874 μs | 0.8701 μs |  0.86 |    0.19 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.451 μs |  7.216 μs | 0.3955 μs |  1.16 |    0.16 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.699 μs |  7.976 μs | 0.4372 μs |  0.79 |    0.13 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.255 μs | 13.249 μs | 0.7262 μs |  1.12 |    0.19 |    1 |      64 B |        1.14 |
