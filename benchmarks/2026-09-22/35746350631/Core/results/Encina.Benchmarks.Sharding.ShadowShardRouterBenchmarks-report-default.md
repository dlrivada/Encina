
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.049 μs** |  **9.734 μs** | **0.5335 μs** |  **1.02** |    **0.23** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.133 μs | 10.332 μs | 0.5663 μs |  1.05 |    0.23 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.441 μs |  5.058 μs | 0.2772 μs |  1.49 |    0.25 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.315 μs |  4.323 μs | 0.2370 μs |  1.11 |    0.19 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.593 μs |  8.548 μs | 0.4685 μs |  1.20 |    0.23 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.759 μs** |  **2.058 μs** | **0.1128 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.962 μs | 15.346 μs | 0.8411 μs |  1.32 |    0.20 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.041 μs |  6.725 μs | 0.3686 μs |  1.61 |    0.09 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.171 μs |  8.435 μs | 0.4623 μs |  1.11 |    0.11 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.593 μs |  5.153 μs | 0.2825 μs |  1.22 |    0.07 |    1 |      64 B |        1.14 |
