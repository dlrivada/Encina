
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.706 μs** |  **5.809 μs** | **0.3184 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.670 μs | 13.865 μs | 0.7600 μs |  1.00 |    0.20 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.859 μs | 10.809 μs | 0.5925 μs |  1.32 |    0.17 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 4.310 μs |  9.082 μs | 0.4978 μs |  1.17 |    0.15 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.013 μs |  5.337 μs | 0.2925 μs |  1.09 |    0.11 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.385 μs** | **15.550 μs** | **0.8523 μs** |  **1.02** |    **0.24** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.664 μs |  3.114 μs | 0.1707 μs |  0.86 |    0.14 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.620 μs | 23.493 μs | 1.2878 μs |  1.31 |    0.33 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.921 μs |  6.446 μs | 0.3533 μs |  0.92 |    0.16 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.075 μs |  7.182 μs | 0.3937 μs |  0.95 |    0.17 |    1 |      64 B |        1.14 |
