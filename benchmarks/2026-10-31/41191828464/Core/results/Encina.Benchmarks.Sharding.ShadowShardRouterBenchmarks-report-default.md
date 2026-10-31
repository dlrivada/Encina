
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.101 μs** | **4.285 μs** | **0.2349 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.140 μs | 4.155 μs | 0.2277 μs |  1.02 |    0.09 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.335 μs | 1.943 μs | 0.1065 μs |  1.40 |    0.09 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 4.006 μs | 4.559 μs | 0.2499 μs |  1.30 |    0.11 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.433 μs | 2.921 μs | 0.1601 μs |  1.11 |    0.08 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.554 μs** | **2.010 μs** | **0.1102 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.471 μs | 1.519 μs | 0.0833 μs |  1.26 |    0.04 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.813 μs | 6.446 μs | 0.3533 μs |  1.36 |    0.09 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.542 μs | 1.196 μs | 0.0656 μs |  1.00 |    0.03 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.845 μs | 2.917 μs | 0.1599 μs |  1.08 |    0.05 |    1 |      64 B |        1.14 |
