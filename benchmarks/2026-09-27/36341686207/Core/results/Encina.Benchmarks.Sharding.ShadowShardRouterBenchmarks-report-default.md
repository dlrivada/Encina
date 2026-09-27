
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **1.496 μs** | **19.924 μs** | **1.0921 μs** | **1.0220 μs** |  **1.36** |    **1.16** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.399 μs | 20.236 μs | 1.1092 μs | 0.9910 μs |  1.27 |    1.15 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.277 μs | 18.864 μs | 1.0340 μs | 2.9190 μs |  2.98 |    1.71 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.319 μs | 22.554 μs | 1.2363 μs | 0.8315 μs |  1.20 |    1.22 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.818 μs | 17.197 μs | 0.9426 μs | 2.4640 μs |  2.56 |    1.50 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.297 μs** | **10.611 μs** | **0.5816 μs** | **3.1400 μs** |  **1.02** |    **0.22** |    **4** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 1.880 μs | 15.870 μs | 0.8699 μs | 1.3920 μs |  0.58 |    0.25 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 2.822 μs | 21.026 μs | 1.1525 μs | 2.2175 μs |  0.87 |    0.34 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 1.629 μs | 19.508 μs | 1.0693 μs | 1.0220 μs |  0.50 |    0.30 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 1.794 μs | 23.284 μs | 1.2763 μs | 1.2675 μs |  0.56 |    0.35 |    2 |      64 B |        1.14 |
