
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.812 μs** |  **4.313 μs** | **0.2364 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.302 μs |  7.420 μs | 0.4067 μs |  0.82 |    0.14 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.545 μs |  6.189 μs | 0.3392 μs |  1.27 |    0.14 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.440 μs |  5.473 μs | 0.3000 μs |  0.87 |    0.11 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.641 μs |  6.879 μs | 0.3771 μs |  0.94 |    0.14 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.567 μs** |  **4.883 μs** | **0.2676 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.813 μs |  6.131 μs | 0.3361 μs |  1.07 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.756 μs | 10.787 μs | 0.5913 μs |  1.34 |    0.17 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.128 μs |  5.060 μs | 0.2774 μs |  0.88 |    0.09 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.627 μs | 13.932 μs | 0.7636 μs |  1.02 |    0.20 |    1 |      64 B |        1.14 |
