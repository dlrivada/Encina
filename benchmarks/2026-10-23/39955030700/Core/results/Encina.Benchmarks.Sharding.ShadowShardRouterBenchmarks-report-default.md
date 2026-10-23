
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.301 μs** | **3.9543 μs** | **0.2167 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.270 μs | 0.5161 μs | 0.0283 μs |  0.99 |    0.06 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.245 μs | 4.6594 μs | 0.2554 μs |  1.29 |    0.10 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.290 μs | 3.4350 μs | 0.1883 μs |  1.00 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.592 μs | 2.3392 μs | 0.1282 μs |  1.09 |    0.07 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.424 μs** | **4.2013 μs** | **0.2303 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.422 μs | 2.6670 μs | 0.1462 μs |  1.00 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.778 μs | 6.9069 μs | 0.3786 μs |  1.40 |    0.13 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.469 μs | 5.0583 μs | 0.2773 μs |  1.02 |    0.09 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.837 μs | 2.6311 μs | 0.1442 μs |  1.12 |    0.08 |    1 |      64 B |        1.14 |
