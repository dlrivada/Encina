
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **4.201 μs** |  **1.604 μs** | **0.0879 μs** |  **1.00** |    **0.03** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.270 μs | 15.304 μs | 0.8388 μs |  0.78 |    0.17 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.920 μs |  9.408 μs | 0.5157 μs |  1.17 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 4.123 μs |  9.601 μs | 0.5263 μs |  0.98 |    0.11 |    2 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.684 μs | 26.855 μs | 1.4720 μs |  1.12 |    0.30 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.956 μs** | **15.893 μs** | **0.8711 μs** |  **1.03** |    **0.27** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.854 μs | 12.227 μs | 0.6702 μs |  1.26 |    0.27 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.867 μs | 17.412 μs | 0.9544 μs |  1.53 |    0.34 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 6.672 μs | 11.939 μs | 0.6544 μs |  1.74 |    0.34 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 6.820 μs | 13.248 μs | 0.7262 μs |  1.78 |    0.35 |    2 |      64 B |        1.14 |
