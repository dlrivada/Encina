
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.023 μs** | **0.1089 μs** | **0.1454 μs** | **2.995 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.223 μs | 0.2301 μs | 0.3300 μs | 3.114 μs |  1.07 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.120 μs | 0.1714 μs | 0.2458 μs | 4.032 μs |  1.37 |    0.10 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.237 μs | 0.2740 μs | 0.3929 μs | 3.086 μs |  1.07 |    0.14 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.331 μs | 0.1324 μs | 0.1767 μs | 3.257 μs |  1.10 |    0.08 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.286 μs** | **0.1230 μs** | **0.1725 μs** | **3.217 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.683 μs | 0.3369 μs | 0.4612 μs | 3.387 μs |  1.12 |    0.15 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.562 μs | 0.0912 μs | 0.1248 μs | 4.576 μs |  1.39 |    0.08 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.575 μs | 0.1609 μs | 0.2149 μs | 3.537 μs |  1.09 |    0.08 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.905 μs | 0.1053 μs | 0.1476 μs | 3.877 μs |  1.19 |    0.07 |    2 |      64 B |        1.14 |
