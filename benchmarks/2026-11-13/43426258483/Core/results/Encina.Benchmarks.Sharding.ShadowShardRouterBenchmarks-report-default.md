
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.058 μs** | **12.524 μs** | **0.6865 μs** | **3.0540 μs** |  **1.04** |    **0.29** |    **4** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.489 μs | 19.417 μs | 1.0643 μs | 0.9620 μs |  0.50 |    0.33 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 2.244 μs | 26.948 μs | 1.4771 μs | 1.6930 μs |  0.76 |    0.47 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.674 μs | 21.980 μs | 1.2048 μs | 1.2665 μs |  0.57 |    0.38 |    2 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 1.980 μs | 28.151 μs | 1.5431 μs | 1.2120 μs |  0.67 |    0.48 |    3 |      64 B |        1.14 |
                                          |            |          |           |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **1.947 μs** | **18.944 μs** | **1.0384 μs** | **1.3830 μs** |  **1.17** |    **0.70** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 1.931 μs | 20.792 μs | 1.1397 μs | 1.3965 μs |  1.16 |    0.75 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 3.321 μs | 17.558 μs | 0.9624 μs | 3.1545 μs |  2.00 |    0.88 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 1.732 μs | 23.281 μs | 1.2761 μs | 1.2215 μs |  1.04 |    0.79 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 2.104 μs | 26.637 μs | 1.4601 μs | 1.5970 μs |  1.26 |    0.92 |    1 |      64 B |        1.14 |
