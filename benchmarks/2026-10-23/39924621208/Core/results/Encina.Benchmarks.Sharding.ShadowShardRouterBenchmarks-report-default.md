
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.625 μs** | **5.584 μs** | **0.3061 μs** |  **1.00** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.025 μs | 3.014 μs | 0.1652 μs |  0.84 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.457 μs | 7.956 μs | 0.4361 μs |  1.24 |    0.14 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.160 μs | 2.803 μs | 0.1537 μs |  0.88 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.415 μs | 5.179 μs | 0.2839 μs |  0.95 |    0.10 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.117 μs** | **4.189 μs** | **0.2296 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.484 μs | 3.000 μs | 0.1644 μs |  0.85 |    0.05 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.692 μs | 4.487 μs | 0.2459 μs |  1.14 |    0.07 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.386 μs | 1.740 μs | 0.0954 μs |  0.82 |    0.04 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.593 μs | 4.621 μs | 0.2533 μs |  1.12 |    0.07 |    1 |      64 B |        1.14 |
