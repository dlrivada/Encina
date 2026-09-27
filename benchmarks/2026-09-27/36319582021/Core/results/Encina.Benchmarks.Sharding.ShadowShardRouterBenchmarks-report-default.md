
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.474 μs** |  **5.372 μs** | **0.2944 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.418 μs |  3.486 μs | 0.1911 μs |  0.99 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.163 μs |  8.845 μs | 0.4848 μs |  1.29 |    0.21 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.736 μs |  7.996 μs | 0.4383 μs |  1.12 |    0.19 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.797 μs |  6.749 μs | 0.3699 μs |  1.14 |    0.17 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.774 μs** |  **3.047 μs** | **0.1670 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.897 μs |  5.054 μs | 0.2770 μs |  1.05 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 3.956 μs |  8.190 μs | 0.4489 μs |  1.43 |    0.16 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.743 μs |  1.770 μs | 0.0970 μs |  0.99 |    0.06 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.526 μs | 12.543 μs | 0.6875 μs |  1.27 |    0.23 |    1 |      64 B |        1.14 |
