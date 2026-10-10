
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.118 μs** |  **2.658 μs** | **0.1457 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.442 μs | 10.464 μs | 0.5736 μs |  1.11 |    0.17 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.068 μs |  3.491 μs | 0.1914 μs |  1.31 |    0.08 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.292 μs |  1.014 μs | 0.0556 μs |  1.06 |    0.05 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.612 μs |  2.854 μs | 0.1565 μs |  1.16 |    0.06 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.650 μs** |  **5.305 μs** | **0.2908 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.446 μs |  5.777 μs | 0.3166 μs |  1.22 |    0.11 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.806 μs |  4.809 μs | 0.2636 μs |  1.32 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.442 μs |  4.123 μs | 0.2260 μs |  0.95 |    0.08 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.196 μs | 18.104 μs | 0.9924 μs |  1.43 |    0.26 |    2 |      64 B |        1.14 |
