
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **1.407 μs** | **18.477 μs** | **1.0128 μs** | **1.0970 μs** |  **1.41** |    **1.28** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.499 μs | 19.577 μs | 1.0731 μs | 1.1020 μs |  1.50 |    1.36 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 2.184 μs | 23.438 μs | 1.2847 μs | 1.7630 μs |  2.19 |    1.78 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.474 μs | 22.558 μs | 1.2365 μs | 0.9270 μs |  1.48 |    1.48 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 1.741 μs | 22.398 μs | 1.2277 μs | 1.3765 μs |  1.75 |    1.57 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **1.724 μs** | **21.188 μs** | **1.1614 μs** | **1.2370 μs** |  **1.30** |    **1.02** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 1.801 μs | 20.101 μs | 1.1018 μs | 1.2570 μs |  1.36 |    1.01 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 2.622 μs | 24.855 μs | 1.3624 μs | 2.1780 μs |  1.98 |    1.33 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 1.743 μs | 18.880 μs | 1.0349 μs | 1.4420 μs |  1.32 |    0.96 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.276 μs | 18.118 μs | 0.9931 μs | 2.9095 μs |  2.47 |    1.35 |    3 |      64 B |        1.14 |
