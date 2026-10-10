
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          |  **4.588 μs** |  **7.322 μs** | **0.4013 μs** |  **1.01** |    **0.11** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          |  3.780 μs | 12.412 μs | 0.6804 μs |  0.83 |    0.14 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          |  5.538 μs | 11.669 μs | 0.6396 μs |  1.21 |    0.15 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          |  4.255 μs | 24.214 μs | 1.3272 μs |  0.93 |    0.26 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          |  3.799 μs | 13.859 μs | 0.7597 μs |  0.83 |    0.16 |    1 |      64 B |        1.14 |
                                          |            |           |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         |  **5.455 μs** | **17.471 μs** | **0.9577 μs** |  **1.02** |    **0.22** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         |  6.065 μs | 34.486 μs | 1.8903 μs |  1.13 |    0.35 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 10.868 μs | 29.389 μs | 1.6109 μs |  2.03 |    0.40 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         |  5.511 μs | 14.168 μs | 0.7766 μs |  1.03 |    0.20 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         |  7.142 μs | 26.996 μs | 1.4797 μs |  1.34 |    0.31 |    1 |      64 B |        1.14 |
