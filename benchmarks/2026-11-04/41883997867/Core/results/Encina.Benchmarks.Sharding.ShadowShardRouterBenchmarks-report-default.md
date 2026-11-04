
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **1.424 μs** | **21.757 μs** | **1.1926 μs** | **0.8070 μs** |  **1.47** |    **1.39** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.635 μs | 16.943 μs | 0.9287 μs | 1.1810 μs |  1.69 |    1.25 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.554 μs | 21.750 μs | 1.1922 μs | 3.1200 μs |  3.67 |    2.19 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.923 μs | 31.600 μs | 1.7321 μs | 1.0715 μs |  1.98 |    1.98 |    2 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.477 μs | 29.329 μs | 1.6076 μs | 2.4535 μs |  2.56 |    2.04 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **1.794 μs** | **17.099 μs** | **0.9372 μs** | **1.4265 μs** |  **1.17** |    **0.72** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 1.813 μs | 18.577 μs | 1.0183 μs | 1.3130 μs |  1.19 |    0.76 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 2.781 μs | 23.604 μs | 1.2938 μs | 2.2430 μs |  1.82 |    1.04 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.069 μs | 16.398 μs | 0.8989 μs | 1.7180 μs |  1.35 |    0.74 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 2.030 μs | 24.378 μs | 1.3362 μs | 1.6125 μs |  1.33 |    0.95 |    2 |      64 B |        1.14 |
