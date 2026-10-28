
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.350 μs** |  **2.777 μs** | **0.1522 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.548 μs |  4.043 μs | 0.2216 μs |  1.09 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.180 μs |  8.134 μs | 0.4459 μs |  1.36 |    0.18 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.662 μs | 10.711 μs | 0.5871 μs |  1.14 |    0.23 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.523 μs |  7.492 μs | 0.4107 μs |  1.08 |    0.16 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.917 μs** |  **4.239 μs** | **0.2323 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.830 μs |  5.404 μs | 0.2962 μs |  0.97 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.069 μs |  8.439 μs | 0.4625 μs |  1.40 |    0.17 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.750 μs |  1.851 μs | 0.1015 μs |  0.95 |    0.07 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.492 μs |  4.190 μs | 0.2297 μs |  1.20 |    0.11 |    2 |      64 B |        1.14 |
