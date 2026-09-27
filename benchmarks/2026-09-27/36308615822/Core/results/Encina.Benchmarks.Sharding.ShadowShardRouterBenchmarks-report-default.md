
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.063 μs** |  **2.114 μs** | **0.1159 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.280 μs |  5.806 μs | 0.3183 μs |  1.07 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.573 μs | 10.342 μs | 0.5669 μs |  1.49 |    0.17 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.606 μs | 16.021 μs | 0.8781 μs |  1.18 |    0.25 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.389 μs |  1.107 μs | 0.0607 μs |  1.11 |    0.04 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.393 μs** |  **1.763 μs** | **0.0966 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.686 μs |  4.764 μs | 0.2612 μs |  1.09 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.422 μs |  8.226 μs | 0.4509 μs |  1.89 |    0.12 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.410 μs |  2.976 μs | 0.1631 μs |  1.01 |    0.05 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.906 μs |  1.590 μs | 0.0872 μs |  1.15 |    0.04 |    1 |      64 B |        1.14 |
