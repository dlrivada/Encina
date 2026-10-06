
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.384 μs** | **5.769 μs** | **0.3162 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.678 μs | 6.322 μs | 0.3465 μs |  1.09 |    0.13 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.465 μs | 9.076 μs | 0.4975 μs |  1.33 |    0.17 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.142 μs | 4.817 μs | 0.2641 μs |  0.93 |    0.10 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.243 μs | 3.151 μs | 0.1727 μs |  0.96 |    0.09 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.509 μs** | **3.451 μs** | **0.1891 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.960 μs | 7.421 μs | 0.4068 μs |  1.13 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.077 μs | 1.827 μs | 0.1002 μs |  1.45 |    0.07 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.008 μs | 7.301 μs | 0.4002 μs |  1.14 |    0.11 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.455 μs | 5.468 μs | 0.2997 μs |  1.27 |    0.09 |    1 |      64 B |        1.14 |
