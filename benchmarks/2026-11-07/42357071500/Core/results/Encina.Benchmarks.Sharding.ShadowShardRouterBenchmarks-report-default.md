
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.467 μs** | **26.529 μs** | **1.4541 μs** | **2.998 μs** |  **1.11** |    **0.55** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.519 μs | 29.004 μs | 1.5898 μs | 2.845 μs |  1.13 |    0.59 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.420 μs | 43.723 μs | 2.3966 μs | 3.474 μs |  1.42 |    0.83 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.482 μs | 24.015 μs | 1.3163 μs | 3.109 μs |  1.12 |    0.52 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.793 μs | 19.123 μs | 1.0482 μs | 4.566 μs |  1.54 |    0.58 |    3 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **6.499 μs** | **20.501 μs** | **1.1237 μs** | **5.963 μs** |  **1.02** |    **0.21** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 7.109 μs | 14.305 μs | 0.7841 μs | 7.168 μs |  1.11 |    0.19 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.232 μs | 14.296 μs | 0.7836 μs | 5.921 μs |  0.98 |    0.17 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.388 μs | 20.160 μs | 1.1050 μs | 3.822 μs |  0.69 |    0.18 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 6.556 μs | 19.606 μs | 1.0747 μs | 6.532 μs |  1.03 |    0.20 |    2 |      64 B |        1.14 |
