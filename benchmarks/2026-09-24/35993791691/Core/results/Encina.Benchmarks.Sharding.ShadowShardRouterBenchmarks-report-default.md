
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.203 μs** |  **5.286 μs** | **0.2898 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.198 μs |  6.762 μs | 0.3706 μs |  1.00 |    0.13 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.158 μs | 10.737 μs | 0.5885 μs |  1.31 |    0.19 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.163 μs |  8.186 μs | 0.4487 μs |  0.99 |    0.14 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.414 μs |  8.650 μs | 0.4741 μs |  1.07 |    0.15 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.717 μs** |  **8.439 μs** | **0.4626 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.939 μs |  4.935 μs | 0.2705 μs |  1.07 |    0.13 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.111 μs |  8.993 μs | 0.4930 μs |  1.39 |    0.18 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.656 μs |  3.014 μs | 0.1652 μs |  0.99 |    0.11 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.086 μs | 13.321 μs | 0.7302 μs |  1.38 |    0.22 |    2 |      64 B |        1.14 |
