
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.968 μs** |  **4.495 μs** | **0.2464 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.574 μs |  4.050 μs | 0.2220 μs |  0.90 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.927 μs | 14.328 μs | 0.7854 μs |  1.24 |    0.18 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.446 μs |  5.532 μs | 0.3032 μs |  0.87 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.583 μs |  9.277 μs | 0.5085 μs |  1.16 |    0.13 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.762 μs** |  **1.204 μs** | **0.0660 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.871 μs |  4.028 μs | 0.2208 μs |  0.81 |    0.04 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 7.858 μs | 13.267 μs | 0.7272 μs |  1.65 |    0.13 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.981 μs |  5.216 μs | 0.2859 μs |  0.84 |    0.05 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 6.241 μs |  6.588 μs | 0.3611 μs |  1.31 |    0.07 |    2 |      64 B |        1.14 |
