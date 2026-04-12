
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.113 μs** | **0.0530 μs** | **0.0726 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.089 μs | 0.0579 μs | 0.0830 μs |  0.99 |    0.03 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.058 μs | 0.0677 μs | 0.0927 μs |  1.30 |    0.04 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.108 μs | 0.0478 μs | 0.0670 μs |  1.00 |    0.03 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.348 μs | 0.0423 μs | 0.0607 μs |  1.08 |    0.03 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.102 μs** | **0.0857 μs** | **0.1283 μs** |  **1.00** |    **0.04** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.688 μs | 0.2807 μs | 0.4025 μs |  0.90 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.042 μs | 0.2232 μs | 0.3201 μs |  1.23 |    0.09 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.360 μs | 0.0550 μs | 0.0806 μs |  0.82 |    0.03 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.845 μs | 0.0372 μs | 0.0534 μs |  0.94 |    0.03 |    1 |      64 B |        1.14 |
