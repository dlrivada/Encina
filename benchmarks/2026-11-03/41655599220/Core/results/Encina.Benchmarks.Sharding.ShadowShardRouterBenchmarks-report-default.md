
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **4.305 μs** | **7.934 μs** | **0.4349 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.216 μs | 4.490 μs | 0.2461 μs |  0.75 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.269 μs | 2.460 μs | 0.1349 μs |  1.00 |    0.09 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.383 μs | 2.491 μs | 0.1365 μs |  0.79 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.987 μs | 5.576 μs | 0.3056 μs |  0.93 |    0.11 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.877 μs** | **5.751 μs** | **0.3152 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.800 μs | 2.936 μs | 0.1609 μs |  0.98 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.570 μs | 7.803 μs | 0.4277 μs |  1.44 |    0.14 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.759 μs | 7.773 μs | 0.4261 μs |  1.23 |    0.13 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.999 μs | 8.650 μs | 0.4741 μs |  1.30 |    0.14 |    2 |      64 B |        1.14 |
