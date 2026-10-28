
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.306 μs** |  **3.749 μs** | **0.2055 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.128 μs |  7.174 μs | 0.3933 μs |  0.95 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.177 μs |  5.761 μs | 0.3158 μs |  1.27 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.561 μs |  9.924 μs | 0.5439 μs |  1.08 |    0.16 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.510 μs | 12.152 μs | 0.6661 μs |  1.06 |    0.18 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.136 μs** |  **6.017 μs** | **0.3298 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.844 μs |  5.292 μs | 0.2901 μs |  0.93 |    0.09 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.279 μs |  6.614 μs | 0.3625 μs |  1.28 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.874 μs |  3.214 μs | 0.1762 μs |  0.94 |    0.07 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.969 μs | 11.688 μs | 0.6407 μs |  1.21 |    0.16 |    2 |      64 B |        1.14 |
