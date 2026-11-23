
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.660 μs** | **37.760 μs** | **2.0698 μs** | **1.655 μs** |  **1.40** |    **1.25** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.900 μs | 28.796 μs | 1.5784 μs | 1.276 μs |  1.00 |    0.93 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 6.726 μs | 13.971 μs | 0.7658 μs | 6.375 μs |  3.54 |    1.78 |    4 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 5.792 μs | 19.397 μs | 1.0632 μs | 6.035 μs |  3.05 |    1.59 |    4 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.858 μs |  6.509 μs | 0.3568 μs | 4.666 μs |  2.56 |    1.27 |    3 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **5.619 μs** | **17.287 μs** | **0.9476 μs** | **5.088 μs** |  **1.02** |    **0.20** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 5.886 μs | 15.998 μs | 0.8769 μs | 5.759 μs |  1.07 |    0.20 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.535 μs | 18.872 μs | 1.0344 μs | 6.500 μs |  1.18 |    0.23 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 5.789 μs | 26.248 μs | 1.4388 μs | 5.909 μs |  1.05 |    0.27 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 6.317 μs |  7.393 μs | 0.4052 μs | 6.476 μs |  1.14 |    0.17 |    1 |      64 B |        1.14 |
