
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.701 μs** | **0.1348 μs** | **0.1845 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.043 μs | 0.2331 μs | 0.3268 μs |  1.13 |    0.14 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.071 μs | 0.3013 μs | 0.4322 μs |  1.51 |    0.19 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.030 μs | 0.1692 μs | 0.2426 μs |  1.13 |    0.12 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.500 μs | 0.3779 μs | 0.5298 μs |  1.30 |    0.21 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.166 μs** | **0.2067 μs** | **0.2965 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.196 μs | 0.2406 μs | 0.3450 μs |  1.01 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.945 μs | 0.3364 μs | 0.4931 μs |  1.43 |    0.15 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.171 μs | 0.1875 μs | 0.2688 μs |  1.01 |    0.09 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.090 μs | 0.1794 μs | 0.2516 μs |  1.23 |    0.10 |    2 |      64 B |        1.14 |
