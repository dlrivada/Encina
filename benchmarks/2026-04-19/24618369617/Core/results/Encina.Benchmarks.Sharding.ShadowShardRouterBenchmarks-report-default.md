
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.277 μs** | **0.3865 μs** | **0.5543 μs** | **3.149 μs** |  **1.03** |    **0.25** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.438 μs | 0.3658 μs | 0.5247 μs | 3.425 μs |  1.08 |    0.25 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.590 μs | 0.1642 μs | 0.2302 μs | 3.572 μs |  1.13 |    0.21 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.874 μs | 0.1266 μs | 0.1815 μs | 2.859 μs |  0.90 |    0.16 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.359 μs | 0.3098 μs | 0.4240 μs | 3.341 μs |  1.05 |    0.22 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.436 μs** | **0.2109 μs** | **0.2887 μs** | **3.387 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.481 μs | 0.8297 μs | 1.1358 μs | 5.149 μs |  1.31 |    0.34 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.654 μs | 0.4129 μs | 0.6052 μs | 4.468 μs |  1.36 |    0.20 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.267 μs | 0.8819 μs | 1.2928 μs | 3.567 μs |  1.25 |    0.39 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.045 μs | 0.2519 μs | 0.3693 μs | 3.962 μs |  1.18 |    0.14 |    2 |      64 B |        1.14 |
