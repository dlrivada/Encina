
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.938 μs** |  **7.781 μs** | **0.4265 μs** |  **1.01** |    **0.18** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.152 μs |  6.978 μs | 0.3825 μs |  1.09 |    0.18 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.247 μs |  6.624 μs | 0.3631 μs |  1.47 |    0.21 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.540 μs |  4.763 μs | 0.2611 μs |  1.22 |    0.17 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.702 μs | 10.166 μs | 0.5572 μs |  1.28 |    0.23 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.605 μs** |  **5.259 μs** | **0.2883 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.534 μs |  7.070 μs | 0.3875 μs |  0.98 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.235 μs |  7.807 μs | 0.4279 μs |  1.46 |    0.15 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.539 μs |  5.028 μs | 0.2756 μs |  0.99 |    0.10 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.885 μs |  5.710 μs | 0.3130 μs |  1.08 |    0.11 |    1 |      64 B |        1.14 |
