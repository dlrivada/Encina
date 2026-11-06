
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **1.771 μs** | **14.237 μs** | **0.7804 μs** | **1.3870 μs** |  **1.12** |    **0.56** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.896 μs | 28.121 μs | 1.5414 μs | 1.0310 μs |  1.20 |    0.95 |    3 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 2.080 μs | 26.848 μs | 1.4716 μs | 1.6620 μs |  1.31 |    0.93 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.783 μs | 30.065 μs | 1.6480 μs | 0.8720 μs |  1.12 |    1.00 |    3 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 1.384 μs | 21.756 μs | 1.1925 μs | 0.8560 μs |  0.87 |    0.73 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **1.816 μs** | **19.308 μs** | **1.0583 μs** | **1.3720 μs** |  **1.22** |    **0.82** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.099 μs | 22.144 μs | 1.2138 μs | 1.6925 μs |  1.41 |    0.95 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.602 μs | 10.805 μs | 0.5923 μs | 4.3810 μs |  3.08 |    1.30 |    5 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.684 μs | 27.029 μs | 1.4816 μs | 2.0530 μs |  1.80 |    1.17 |    3 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.087 μs | 31.677 μs | 1.7363 μs | 2.6390 μs |  2.07 |    1.37 |    4 |      64 B |        1.14 |
