
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.218 μs** | **1.109 μs** | **0.0608 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.121 μs | 4.261 μs | 0.2336 μs |  0.97 |    0.06 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 5.106 μs | 4.386 μs | 0.2404 μs |  1.59 |    0.07 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.237 μs | 2.220 μs | 0.1217 μs |  1.01 |    0.04 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.536 μs | 1.590 μs | 0.0872 μs |  1.10 |    0.03 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.185 μs** | **2.199 μs** | **0.1206 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.248 μs | 2.697 μs | 0.1479 μs |  1.02 |    0.04 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.740 μs | 3.010 μs | 0.1650 μs |  1.37 |    0.05 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.841 μs | 6.668 μs | 0.3655 μs |  0.92 |    0.08 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.049 μs | 2.597 μs | 0.1423 μs |  0.97 |    0.04 |    1 |      64 B |        1.14 |
