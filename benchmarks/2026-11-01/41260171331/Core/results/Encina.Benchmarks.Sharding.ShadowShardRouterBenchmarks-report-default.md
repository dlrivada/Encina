
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.043 μs** | **0.1479 μs** | **0.2121 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.350 μs | 0.2628 μs | 0.3769 μs |  1.11 |    0.15 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.357 μs | 0.2851 μs | 0.4088 μs |  1.44 |    0.17 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.347 μs | 0.1899 μs | 0.2662 μs |  1.11 |    0.12 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.403 μs | 0.1013 μs | 0.1387 μs |  1.12 |    0.09 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.700 μs** | **0.3027 μs** | **0.4244 μs** |  **1.01** |    **0.16** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.357 μs | 0.1346 μs | 0.1843 μs |  0.92 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.654 μs | 0.1062 μs | 0.1489 μs |  1.27 |    0.14 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.214 μs | 0.0843 μs | 0.1182 μs |  0.88 |    0.10 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.941 μs | 0.0601 μs | 0.0862 μs |  1.08 |    0.12 |    1 |      64 B |        1.14 |
