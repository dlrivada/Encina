
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.225 μs** | **1.850 μs** | **0.1014 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.045 μs | 4.739 μs | 0.2597 μs |  0.94 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.187 μs | 4.494 μs | 0.2463 μs |  1.30 |    0.07 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.273 μs | 2.718 μs | 0.1490 μs |  1.02 |    0.05 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.630 μs | 2.556 μs | 0.1401 μs |  1.13 |    0.05 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.559 μs** | **2.487 μs** | **0.1363 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.940 μs | 8.636 μs | 0.4734 μs |  1.11 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.796 μs | 2.113 μs | 0.1158 μs |  1.63 |    0.06 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.400 μs | 2.390 μs | 0.1310 μs |  0.96 |    0.05 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.948 μs | 2.574 μs | 0.1411 μs |  1.11 |    0.05 |    1 |      64 B |        1.14 |
