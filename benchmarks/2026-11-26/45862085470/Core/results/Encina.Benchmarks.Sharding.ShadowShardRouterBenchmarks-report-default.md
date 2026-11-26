
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **1.991 μs** | **28.120 μs** | **1.5414 μs** | **1.446 μs** |  **1.47** |    **1.41** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.676 μs | 19.146 μs | 1.0495 μs | 1.151 μs |  1.24 |    1.05 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.435 μs | 20.346 μs | 1.1152 μs | 3.025 μs |  2.54 |    1.69 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.762 μs | 20.848 μs | 1.1428 μs | 1.351 μs |  1.30 |    1.13 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.764 μs | 17.257 μs | 0.9459 μs | 2.494 μs |  2.04 |    1.37 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.295 μs** | **21.806 μs** | **1.1952 μs** | **1.887 μs** |  **1.18** |    **0.73** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 1.936 μs | 18.158 μs | 0.9953 μs | 1.502 μs |  1.00 |    0.61 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 2.761 μs | 24.487 μs | 1.3422 μs | 2.083 μs |  1.42 |    0.84 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 1.868 μs | 18.998 μs | 1.0413 μs | 1.318 μs |  0.96 |    0.62 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 2.064 μs | 22.862 μs | 1.2531 μs | 1.443 μs |  1.06 |    0.72 |    1 |      64 B |        1.14 |
