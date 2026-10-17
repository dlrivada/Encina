
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.985 μs** | **13.470 μs** | **0.7383 μs** |  **1.02** |    **0.23** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.639 μs | 16.740 μs | 0.9176 μs |  0.93 |    0.25 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 6.551 μs | 34.877 μs | 1.9117 μs |  1.68 |    0.50 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 4.540 μs | 14.262 μs | 0.7818 μs |  1.16 |    0.25 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.833 μs | 13.521 μs | 0.7411 μs |  1.24 |    0.25 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **5.544 μs** |  **9.387 μs** | **0.5145 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 5.684 μs | 10.630 μs | 0.5827 μs |  1.03 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.104 μs | 17.162 μs | 0.9407 μs |  1.11 |    0.17 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 6.613 μs | 17.447 μs | 0.9563 μs |  1.20 |    0.18 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 6.452 μs | 21.323 μs | 1.1688 μs |  1.17 |    0.21 |    1 |      64 B |        1.14 |
