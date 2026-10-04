
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.099 μs** | **0.0583 μs** | **0.0817 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.034 μs | 0.0951 μs | 0.1394 μs |  0.98 |    0.05 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.948 μs | 0.0442 μs | 0.0605 μs |  1.27 |    0.04 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.158 μs | 0.0495 μs | 0.0709 μs |  1.02 |    0.03 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.505 μs | 0.1857 μs | 0.2722 μs |  1.13 |    0.09 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.753 μs** | **0.2798 μs** | **0.4012 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.474 μs | 0.0766 μs | 0.1099 μs |  0.94 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.154 μs | 0.3600 μs | 0.4927 μs |  1.39 |    0.19 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.767 μs | 0.2504 μs | 0.3591 μs |  1.01 |    0.14 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.676 μs | 0.0641 μs | 0.0898 μs |  0.99 |    0.10 |    1 |      64 B |        1.14 |
