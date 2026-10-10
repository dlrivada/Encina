
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.300 μs** | **6.395 μs** | **0.3505 μs** |  **1.01** |    **0.18** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.632 μs | 8.689 μs | 0.4763 μs |  1.16 |    0.23 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.252 μs | 9.549 μs | 0.5234 μs |  1.43 |    0.27 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.473 μs | 4.613 μs | 0.2528 μs |  1.09 |    0.16 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.757 μs | 4.893 μs | 0.2682 μs |  1.22 |    0.18 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.098 μs** | **3.386 μs** | **0.1856 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.908 μs | 7.101 μs | 0.3892 μs |  0.94 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 3.937 μs | 5.924 μs | 0.3247 μs |  1.27 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.657 μs | 3.499 μs | 0.1918 μs |  0.86 |    0.07 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.232 μs | 2.852 μs | 0.1563 μs |  1.05 |    0.07 |    1 |      64 B |        1.14 |
