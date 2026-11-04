
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.335 μs** | **22.639 μs** | **1.2409 μs** |  **1.09** |    **0.48** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.383 μs |  5.377 μs | 0.2947 μs |  0.78 |    0.24 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.360 μs |  9.813 μs | 0.5379 μs |  1.10 |    0.35 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.923 μs |  5.833 μs | 0.3197 μs |  0.95 |    0.29 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.644 μs |  2.994 μs | 0.1641 μs |  0.86 |    0.25 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.948 μs** |  **5.944 μs** | **0.3258 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.923 μs |  2.286 μs | 0.1253 μs |  1.00 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.003 μs |  1.079 μs | 0.0592 μs |  1.37 |    0.13 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.865 μs |  5.279 μs | 0.2894 μs |  0.98 |    0.13 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.712 μs |  5.442 μs | 0.2983 μs |  1.27 |    0.15 |    2 |      64 B |        1.14 |
