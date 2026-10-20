
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.686 μs** |  **4.837 μs** | **0.2651 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 4.166 μs | 18.281 μs | 1.0021 μs |  1.13 |    0.25 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.638 μs | 14.885 μs | 0.8159 μs |  1.26 |    0.21 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.200 μs |  2.693 μs | 0.1476 μs |  0.87 |    0.06 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.494 μs |  3.473 μs | 0.1904 μs |  0.95 |    0.07 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.862 μs** | **13.909 μs** | **0.7624 μs** |  **1.02** |    **0.20** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.496 μs |  4.813 μs | 0.2638 μs |  0.73 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.286 μs | 10.262 μs | 0.5625 μs |  1.11 |    0.18 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.497 μs |  3.770 μs | 0.2066 μs |  0.73 |    0.11 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.014 μs |  8.132 μs | 0.4457 μs |  1.05 |    0.16 |    2 |      64 B |        1.14 |
