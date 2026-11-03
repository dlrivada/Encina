
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.008 μs** | **25.846 μs** | **1.4167 μs** | **2.372 μs** |  **1.14** |    **0.61** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.083 μs | 27.207 μs | 1.4913 μs | 2.337 μs |  1.16 |    0.64 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 6.579 μs | 44.217 μs | 2.4237 μs | 5.374 μs |  2.48 |    1.17 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.593 μs | 43.151 μs | 2.3653 μs | 2.542 μs |  1.36 |    0.93 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.464 μs | 34.875 μs | 1.9116 μs | 2.726 μs |  1.31 |    0.79 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **6.228 μs** | **12.789 μs** | **0.7010 μs** | **5.870 μs** |  **1.01** |    **0.14** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.430 μs | 17.788 μs | 0.9750 μs | 4.157 μs |  0.72 |    0.15 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.704 μs | 23.855 μs | 1.3076 μs | 6.213 μs |  1.09 |    0.21 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.507 μs | 20.858 μs | 1.1433 μs | 4.112 μs |  0.73 |    0.17 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.708 μs | 25.686 μs | 1.4080 μs | 5.971 μs |  0.92 |    0.22 |    2 |      64 B |        1.14 |
