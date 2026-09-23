
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.533 μs** |  **5.943 μs** | **0.3257 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.130 μs |  1.846 μs | 0.1012 μs |  0.89 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.498 μs | 12.244 μs | 0.6711 μs |  1.28 |    0.20 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.221 μs |  1.983 μs | 0.1087 μs |  0.92 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.357 μs |  1.963 μs | 0.1076 μs |  0.96 |    0.08 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.292 μs** |  **1.180 μs** | **0.0647 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.020 μs |  1.381 μs | 0.0757 μs |  0.94 |    0.02 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.765 μs |  3.381 μs | 0.1853 μs |  1.11 |    0.04 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.466 μs |  4.572 μs | 0.2506 μs |  0.81 |    0.05 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.582 μs |  2.745 μs | 0.1504 μs |  1.07 |    0.03 |    1 |      64 B |        1.14 |
