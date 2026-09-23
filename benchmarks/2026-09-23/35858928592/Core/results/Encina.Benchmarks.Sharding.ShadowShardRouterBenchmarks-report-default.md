
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.634 μs** | **10.261 μs** | **0.5624 μs** | **3.501 μs** |  **1.02** |    **0.19** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.369 μs |  8.806 μs | 0.4827 μs | 3.095 μs |  0.94 |    0.17 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.447 μs | 22.785 μs | 1.2489 μs | 4.357 μs |  1.24 |    0.34 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.663 μs |  2.296 μs | 0.1258 μs | 3.646 μs |  1.02 |    0.14 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.612 μs |  9.098 μs | 0.4987 μs | 3.365 μs |  1.01 |    0.18 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **6.804 μs** | **82.430 μs** | **4.5183 μs** | **4.771 μs** |  **1.28** |    **0.98** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.095 μs |  5.837 μs | 0.3199 μs | 4.205 μs |  0.77 |    0.35 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 7.128 μs | 19.301 μs | 1.0580 μs | 6.854 μs |  1.35 |    0.62 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.973 μs |  4.510 μs | 0.2472 μs | 3.856 μs |  0.75 |    0.34 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.049 μs |  3.655 μs | 0.2003 μs | 4.036 μs |  0.76 |    0.34 |    1 |      64 B |        1.14 |
