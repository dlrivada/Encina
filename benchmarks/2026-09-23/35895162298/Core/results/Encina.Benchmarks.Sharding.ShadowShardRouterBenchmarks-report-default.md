
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.008 μs** | **4.921 μs** | **0.2697 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.099 μs | 4.859 μs | 0.2663 μs |  1.04 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.001 μs | 5.305 μs | 0.2908 μs |  1.34 |    0.13 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.167 μs | 3.418 μs | 0.1873 μs |  1.06 |    0.10 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.260 μs | 2.082 μs | 0.1141 μs |  1.09 |    0.09 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.210 μs** | **4.454 μs** | **0.2442 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.267 μs | 3.956 μs | 0.2169 μs |  0.78 |    0.06 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.598 μs | 6.048 μs | 0.3315 μs |  1.09 |    0.09 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.392 μs | 1.836 μs | 0.1006 μs |  0.81 |    0.05 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.843 μs | 3.304 μs | 0.1811 μs |  0.92 |    0.06 |    1 |      64 B |        1.14 |
