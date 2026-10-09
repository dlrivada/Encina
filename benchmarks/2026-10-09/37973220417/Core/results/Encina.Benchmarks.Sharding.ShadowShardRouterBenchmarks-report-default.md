
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.430 μs** |  **3.103 μs** | **0.1701 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.256 μs |  2.225 μs | 0.1220 μs |  0.95 |    0.05 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.215 μs |  3.424 μs | 0.1877 μs |  1.23 |    0.07 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.754 μs |  3.660 μs | 0.2006 μs |  1.10 |    0.07 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.205 μs |  3.499 μs | 0.1918 μs |  1.23 |    0.07 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.197 μs** |  **5.809 μs** | **0.3184 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.580 μs |  2.797 μs | 0.1533 μs |  0.86 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.611 μs |  4.782 μs | 0.2621 μs |  1.34 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 7.259 μs | 19.846 μs | 1.0878 μs |  1.74 |    0.25 |    3 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 6.903 μs | 30.916 μs | 1.6946 μs |  1.65 |    0.37 |    3 |      64 B |        1.14 |
