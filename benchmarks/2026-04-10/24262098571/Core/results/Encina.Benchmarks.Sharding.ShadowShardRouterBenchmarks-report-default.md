
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.881 μs** | **23.246 μs** | **1.2742 μs** |  **1.06** |    **0.40** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.055 μs |  2.097 μs | 0.1149 μs |  0.84 |    0.20 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.088 μs |  4.665 μs | 0.2557 μs |  1.12 |    0.28 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.263 μs |  1.889 μs | 0.1035 μs |  0.89 |    0.22 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.466 μs |  5.250 μs | 0.2878 μs |  0.95 |    0.24 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.523 μs** |  **1.899 μs** | **0.1041 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.417 μs |  2.815 μs | 0.1543 μs |  0.97 |    0.05 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.916 μs |  4.374 μs | 0.2398 μs |  1.40 |    0.07 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.945 μs | 19.409 μs | 1.0639 μs |  1.12 |    0.26 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.632 μs |  4.197 μs | 0.2301 μs |  1.32 |    0.07 |    2 |      64 B |        1.14 |
