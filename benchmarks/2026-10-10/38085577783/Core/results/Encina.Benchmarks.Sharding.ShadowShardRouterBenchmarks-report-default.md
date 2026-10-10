
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.493 μs** | **11.954 μs** | **0.6552 μs** |  **1.02** |    **0.23** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.292 μs |  1.740 μs | 0.0954 μs |  0.96 |    0.14 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 5.609 μs | 22.424 μs | 1.2291 μs |  1.64 |    0.40 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.819 μs |  5.728 μs | 0.3140 μs |  1.12 |    0.18 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.433 μs |  1.978 μs | 0.1084 μs |  1.00 |    0.15 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.436 μs** |  **2.856 μs** | **0.1565 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.523 μs |  2.658 μs | 0.1457 μs |  1.03 |    0.05 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.646 μs | 22.741 μs | 1.2465 μs |  1.94 |    0.32 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.392 μs |  1.414 μs | 0.0775 μs |  0.99 |    0.04 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.837 μs |  2.816 μs | 0.1544 μs |  1.12 |    0.06 |    1 |      64 B |        1.14 |
