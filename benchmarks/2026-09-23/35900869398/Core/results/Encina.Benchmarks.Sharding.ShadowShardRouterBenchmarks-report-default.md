
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.150 μs** |  **6.173 μs** | **0.3384 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.132 μs |  7.667 μs | 0.4202 μs |  1.00 |    0.15 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.562 μs | 14.768 μs | 0.8095 μs |  1.46 |    0.26 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.545 μs | 11.190 μs | 0.6134 μs |  1.13 |    0.20 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.429 μs |  9.919 μs | 0.5437 μs |  1.10 |    0.18 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.753 μs** |  **4.947 μs** | **0.2712 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.646 μs | 11.263 μs | 0.6173 μs |  0.97 |    0.15 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.425 μs |  9.041 μs | 0.4955 μs |  1.45 |    0.14 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.479 μs |  2.930 μs | 0.1606 μs |  0.93 |    0.07 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.544 μs |  6.336 μs | 0.3473 μs |  1.21 |    0.11 |    2 |      64 B |        1.14 |
