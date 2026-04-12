
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.838 μs** | **0.0646 μs** | **0.0884 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.917 μs | 0.0539 μs | 0.0719 μs |  1.03 |    0.04 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.265 μs | 0.2231 μs | 0.3200 μs |  1.50 |    0.12 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.987 μs | 0.1035 μs | 0.1451 μs |  1.05 |    0.06 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.346 μs | 0.1966 μs | 0.2820 μs |  1.18 |    0.10 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.219 μs** | **0.0940 μs** | **0.1348 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.337 μs | 0.1901 μs | 0.2787 μs |  1.04 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.521 μs | 0.1488 μs | 0.2086 μs |  1.41 |    0.09 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.501 μs | 0.1172 μs | 0.1680 μs |  1.09 |    0.07 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.260 μs | 0.3281 μs | 0.4809 μs |  1.33 |    0.16 |    2 |      64 B |        1.14 |
