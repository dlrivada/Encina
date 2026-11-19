
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.499 μs** | **11.807 μs** | **0.6472 μs** |  **1.04** |    **0.32** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.805 μs | 11.410 μs | 0.6254 μs |  1.17 |    0.33 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.787 μs | 16.100 μs | 0.8825 μs |  2.00 |    0.52 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.878 μs |  9.599 μs | 0.5261 μs |  1.20 |    0.31 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.978 μs | 12.809 μs | 0.7021 μs |  1.24 |    0.36 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.807 μs** |  **4.554 μs** | **0.2496 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.949 μs |  8.107 μs | 0.4444 μs |  1.41 |    0.17 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.858 μs | 20.751 μs | 1.1374 μs |  2.10 |    0.39 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.279 μs |  2.976 μs | 0.1631 μs |  1.17 |    0.10 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.698 μs | 16.472 μs | 0.9029 μs |  2.04 |    0.32 |    2 |      64 B |        1.14 |
