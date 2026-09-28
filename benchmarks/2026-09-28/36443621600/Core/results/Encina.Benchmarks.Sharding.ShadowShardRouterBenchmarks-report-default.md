
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.542 μs** | **21.099 μs** | **1.1565 μs** |  **1.07** |    **0.41** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.736 μs | 13.503 μs | 0.7401 μs |  1.13 |    0.35 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.794 μs | 13.652 μs | 0.7483 μs |  1.44 |    0.41 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.357 μs |  6.528 μs | 0.3578 μs |  1.01 |    0.27 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.716 μs | 11.602 μs | 0.6359 μs |  1.12 |    0.33 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.553 μs** | **15.664 μs** | **0.8586 μs** |  **1.02** |    **0.23** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 5.999 μs | 27.277 μs | 1.4951 μs |  1.35 |    0.36 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.942 μs | 13.381 μs | 0.7335 μs |  1.56 |    0.29 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.248 μs | 13.564 μs | 0.7435 μs |  0.95 |    0.21 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.287 μs | 11.704 μs | 0.6415 μs |  1.19 |    0.23 |    1 |      64 B |        1.14 |
