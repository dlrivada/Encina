
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.875 μs** | **0.0759 μs** | **0.1064 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.725 μs | 0.1181 μs | 0.1656 μs |  0.95 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.792 μs | 0.1524 μs | 0.2186 μs |  1.32 |    0.09 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.604 μs | 0.1631 μs | 0.2287 μs |  0.91 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.112 μs | 0.1506 μs | 0.2160 μs |  1.08 |    0.08 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.612 μs** | **0.1958 μs** | **0.2870 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.566 μs | 0.1870 μs | 0.2799 μs |  0.99 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.904 μs | 0.1611 μs | 0.2259 μs |  1.37 |    0.12 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.513 μs | 0.2495 μs | 0.3415 μs |  0.98 |    0.12 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.346 μs | 0.1641 μs | 0.2405 μs |  1.21 |    0.11 |    2 |      64 B |        1.14 |
