
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.781 μs** |  **3.749 μs** | **0.2055 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.176 μs |  2.289 μs | 0.1255 μs |  0.84 |    0.05 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.784 μs | 10.008 μs | 0.5486 μs |  1.27 |    0.14 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.763 μs |  7.049 μs | 0.3864 μs |  1.00 |    0.10 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.662 μs |  1.104 μs | 0.0605 μs |  0.97 |    0.05 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.669 μs** |  **1.724 μs** | **0.0945 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.951 μs |  7.849 μs | 0.4303 μs |  1.08 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.401 μs |  6.497 μs | 0.3561 μs |  1.47 |    0.09 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 5.193 μs | 19.531 μs | 1.0706 μs |  1.42 |    0.25 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.451 μs |  9.794 μs | 0.5368 μs |  1.21 |    0.13 |    1 |      64 B |        1.14 |
