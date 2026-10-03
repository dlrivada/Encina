
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.948 μs** |  **4.767 μs** | **0.2613 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.136 μs |  1.420 μs | 0.0778 μs |  1.07 |    0.09 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.652 μs |  5.061 μs | 0.2774 μs |  1.59 |    0.15 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.061 μs |  2.072 μs | 0.1136 μs |  1.04 |    0.09 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.270 μs |  3.492 μs | 0.1914 μs |  1.12 |    0.10 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.335 μs** |  **2.698 μs** | **0.1479 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.963 μs | 25.749 μs | 1.4114 μs |  1.49 |    0.37 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.586 μs |  4.220 μs | 0.2313 μs |  1.38 |    0.08 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.243 μs |  3.715 μs | 0.2036 μs |  0.97 |    0.06 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.980 μs |  2.001 μs | 0.1097 μs |  1.19 |    0.05 |    1 |      64 B |        1.14 |
