
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.634 μs** | **0.6354 μs** | **0.9113 μs** |  **1.04** |    **0.32** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.568 μs | 0.1898 μs | 0.2660 μs |  1.03 |    0.20 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.281 μs | 0.2917 μs | 0.4275 μs |  1.23 |    0.25 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.623 μs | 0.2631 μs | 0.3512 μs |  1.04 |    0.21 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.570 μs | 0.3122 μs | 0.4478 μs |  1.03 |    0.22 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.593 μs** | **0.1996 μs** | **0.2925 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.418 μs | 0.1042 μs | 0.1460 μs |  0.96 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.112 μs | 0.3445 μs | 0.4829 μs |  1.43 |    0.17 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.698 μs | 0.1885 μs | 0.2703 μs |  1.04 |    0.11 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.236 μs | 0.2232 μs | 0.3271 μs |  1.19 |    0.13 |    2 |      64 B |        1.14 |
