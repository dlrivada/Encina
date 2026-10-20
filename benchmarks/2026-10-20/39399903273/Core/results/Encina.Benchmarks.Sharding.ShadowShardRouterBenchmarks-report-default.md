
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.707 μs** |  **5.669 μs** | **0.3107 μs** |  **1.00** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.962 μs | 11.107 μs | 0.6088 μs |  1.07 |    0.16 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.726 μs | 14.856 μs | 0.8143 μs |  1.28 |    0.21 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.603 μs |  6.709 μs | 0.3677 μs |  0.98 |    0.11 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.873 μs | 10.188 μs | 0.5585 μs |  1.05 |    0.15 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.692 μs** |  **2.114 μs** | **0.1159 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.111 μs |  4.435 μs | 0.2431 μs |  1.11 |    0.06 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 7.223 μs | 18.530 μs | 1.0157 μs |  1.96 |    0.24 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.980 μs |  3.861 μs | 0.2117 μs |  1.35 |    0.06 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.799 μs | 18.014 μs | 0.9874 μs |  1.30 |    0.23 |    1 |      64 B |        1.14 |
