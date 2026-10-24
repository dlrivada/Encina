
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.724 μs** |  **6.764 μs** | **0.3708 μs** |  **1.01** |    **0.18** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.718 μs |  8.413 μs | 0.4611 μs |  1.01 |    0.20 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.668 μs |  6.314 μs | 0.3461 μs |  1.36 |    0.21 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.587 μs |  8.603 μs | 0.4716 μs |  0.96 |    0.20 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.991 μs | 12.650 μs | 0.6934 μs |  1.11 |    0.27 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.117 μs** | **11.716 μs** | **0.6422 μs** |  **1.03** |    **0.25** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.171 μs |  7.775 μs | 0.4262 μs |  1.04 |    0.21 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 7.723 μs | 13.251 μs | 0.7263 μs |  2.54 |    0.46 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.897 μs |  2.588 μs | 0.1418 μs |  0.95 |    0.16 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.418 μs |  7.851 μs | 0.4304 μs |  1.12 |    0.22 |    1 |      64 B |        1.14 |
