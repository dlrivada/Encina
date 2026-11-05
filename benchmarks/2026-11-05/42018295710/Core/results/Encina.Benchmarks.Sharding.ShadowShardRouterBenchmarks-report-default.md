
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **1.925 μs** | **28.486 μs** | **1.5614 μs** | **1.477 μs** |  **1.62** |    **1.75** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.841 μs | 29.167 μs | 1.5987 μs | 1.377 μs |  1.55 |    1.74 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 2.243 μs | 36.844 μs | 2.0195 μs | 1.462 μs |  1.89 |    2.18 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.773 μs | 26.692 μs | 1.4631 μs | 1.292 μs |  1.49 |    1.63 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.108 μs | 11.888 μs | 0.6516 μs | 3.075 μs |  2.61 |    1.88 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.220 μs** | **21.514 μs** | **1.1792 μs** | **1.653 μs** |  **1.17** |    **0.71** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.204 μs | 30.920 μs | 1.6948 μs | 2.824 μs |  1.69 |    1.02 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 2.952 μs | 32.307 μs | 1.7709 μs | 2.058 μs |  1.56 |    1.02 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.280 μs | 19.725 μs | 1.0812 μs | 2.768 μs |  1.73 |    0.81 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.312 μs | 38.112 μs | 2.0890 μs | 2.534 μs |  1.75 |    1.19 |    2 |      64 B |        1.14 |
