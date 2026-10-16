
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **4.233 μs** | **25.574 μs** | **1.4018 μs** |  **1.07** |    **0.41** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.106 μs |  1.885 μs | 0.1033 μs |  0.78 |    0.19 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.546 μs | 14.825 μs | 0.8126 μs |  1.15 |    0.34 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.145 μs |  3.340 μs | 0.1831 μs |  0.79 |    0.20 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.075 μs | 19.701 μs | 1.0799 μs |  1.03 |    0.35 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.814 μs** |  **6.331 μs** | **0.3470 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.420 μs |  1.961 μs | 0.1075 μs |  0.90 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.778 μs |  3.807 μs | 0.2087 μs |  1.26 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.457 μs |  2.737 μs | 0.1500 μs |  0.91 |    0.08 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.845 μs |  2.980 μs | 0.1633 μs |  1.01 |    0.09 |    1 |      64 B |        1.14 |
