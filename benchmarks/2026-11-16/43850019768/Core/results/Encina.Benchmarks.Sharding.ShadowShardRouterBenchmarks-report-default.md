
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **4.228 μs** |  **5.706 μs** | **0.3128 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.630 μs | 18.465 μs | 1.0121 μs |  0.86 |    0.22 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.125 μs | 13.493 μs | 0.7396 μs |  0.98 |    0.17 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 5.521 μs | 38.248 μs | 2.0965 μs |  1.31 |    0.44 |    2 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.923 μs |  7.031 μs | 0.3854 μs |  0.93 |    0.10 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.790 μs** |  **9.621 μs** | **0.5273 μs** |  **1.01** |    **0.17** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.912 μs |  8.722 μs | 0.4781 μs |  1.31 |    0.19 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.966 μs | 13.632 μs | 0.7472 μs |  1.59 |    0.26 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 5.862 μs |  3.865 μs | 0.2118 μs |  1.57 |    0.19 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.874 μs |  5.826 μs | 0.3193 μs |  1.57 |    0.20 |    2 |      64 B |        1.14 |
