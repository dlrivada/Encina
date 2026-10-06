
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.667 μs** |  **4.048 μs** | **0.2219 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.532 μs | 10.394 μs | 0.5697 μs |  0.95 |    0.20 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.414 μs |  5.486 μs | 0.3007 μs |  1.29 |    0.13 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.554 μs |  3.780 μs | 0.2072 μs |  0.96 |    0.10 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.794 μs |  4.205 μs | 0.2305 μs |  1.05 |    0.11 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.964 μs** |  **3.059 μs** | **0.1677 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.623 μs |  6.952 μs | 0.3811 μs |  0.89 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.345 μs |  4.904 μs | 0.2688 μs |  1.47 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.908 μs |  4.869 μs | 0.2669 μs |  0.98 |    0.09 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.315 μs |  2.761 μs | 0.1513 μs |  1.12 |    0.07 |    1 |      64 B |        1.14 |
