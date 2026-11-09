
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.495 μs** | **2.716 μs** | **0.1489 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.544 μs | 8.073 μs | 0.4425 μs |  1.02 |    0.16 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.146 μs | 7.152 μs | 0.3920 μs |  1.26 |    0.15 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.644 μs | 3.299 μs | 0.1808 μs |  1.06 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.801 μs | 7.705 μs | 0.4223 μs |  1.13 |    0.16 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.035 μs** | **5.148 μs** | **0.2822 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.854 μs | 4.414 μs | 0.2420 μs |  0.95 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.227 μs | 5.300 μs | 0.2905 μs |  1.40 |    0.15 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.947 μs | 2.381 μs | 0.1305 μs |  0.98 |    0.09 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.298 μs | 8.036 μs | 0.4405 μs |  1.09 |    0.16 |    1 |      64 B |        1.14 |
