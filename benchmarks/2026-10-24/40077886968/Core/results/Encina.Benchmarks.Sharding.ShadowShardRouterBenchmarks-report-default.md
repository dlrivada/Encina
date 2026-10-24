
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **1.627 μs** | **20.701 μs** | **1.1347 μs** | **1.367 μs** |  **1.43** |    **1.32** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.881 μs | 29.079 μs | 1.5939 μs | 1.267 μs |  1.65 |    1.71 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 2.102 μs | 27.707 μs | 1.5187 μs | 1.627 μs |  1.84 |    1.73 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.981 μs | 16.833 μs | 0.9227 μs | 2.683 μs |  2.61 |    1.78 |    2 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 1.827 μs | 25.104 μs | 1.3760 μs | 1.296 μs |  1.60 |    1.55 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.474 μs** | **34.264 μs** | **1.8781 μs** | **1.623 μs** |  **1.39** |    **1.22** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.768 μs | 39.201 μs | 2.1487 μs | 1.552 μs |  1.55 |    1.39 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.910 μs | 27.216 μs | 1.4918 μs | 5.638 μs |  2.76 |    1.59 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.518 μs | 26.190 μs | 1.4356 μs | 1.887 μs |  1.41 |    1.04 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.369 μs | 55.279 μs | 3.0300 μs | 2.374 μs |  1.89 |    1.88 |    2 |      64 B |        1.14 |
