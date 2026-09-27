
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.103 μs** | **2.491 μs** | **0.1365 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.072 μs | 4.099 μs | 0.2247 μs |  0.99 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.064 μs | 4.852 μs | 0.2660 μs |  1.31 |    0.09 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.185 μs | 1.797 μs | 0.0985 μs |  1.03 |    0.05 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.051 μs | 1.594 μs | 0.0874 μs |  1.31 |    0.05 |    2 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.224 μs** | **3.800 μs** | **0.2083 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.467 μs | 1.493 μs | 0.0819 μs |  0.82 |    0.04 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.961 μs | 1.104 μs | 0.0605 μs |  1.41 |    0.06 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.316 μs | 3.214 μs | 0.1762 μs |  0.79 |    0.05 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.779 μs | 2.540 μs | 0.1392 μs |  0.90 |    0.05 |    1 |      64 B |        1.14 |
