
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **4.019 μs** | **10.598 μs** | **0.5809 μs** |  **1.02** |    **0.19** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.424 μs |  7.018 μs | 0.3847 μs |  0.86 |    0.14 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.250 μs |  8.513 μs | 0.4666 μs |  1.07 |    0.18 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.456 μs |  3.481 μs | 0.1908 μs |  0.87 |    0.12 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.421 μs |  3.753 μs | 0.2057 μs |  0.86 |    0.12 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.782 μs** |  **6.322 μs** | **0.3465 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.626 μs |  7.551 μs | 0.4139 μs |  1.23 |    0.13 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.793 μs | 12.705 μs | 0.6964 μs |  1.81 |    0.21 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.905 μs | 10.981 μs | 0.6019 μs |  1.30 |    0.17 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.612 μs |  7.455 μs | 0.4086 μs |  1.23 |    0.13 |    2 |      64 B |        1.14 |
