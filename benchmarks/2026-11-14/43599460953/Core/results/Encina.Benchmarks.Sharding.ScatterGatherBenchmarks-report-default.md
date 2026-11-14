
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.03GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,961.6 ns** |   **304.03 ns** |  **16.66 ns** |  **1.00** |    **0.01** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,976.3 ns | 2,524.08 ns | 138.35 ns |  1.00 |    0.03 |    3 | 0.1450 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  22,061.6 ns | 1,926.62 ns | 105.60 ns |  5.57 |    0.03 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,469.8 ns |   218.20 ns |  11.96 ns |  0.62 |    0.00 |    2 | 0.0916 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     130.0 ns |    20.38 ns |   1.12 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
                                           |            |              |             |           |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **17,706.0 ns** |   **809.23 ns** |  **44.36 ns** | **1.000** |    **0.00** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,846.6 ns |   499.93 ns |  27.40 ns | 0.217 |    0.00 |    3 | 0.1526 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 169,687.3 ns | 7,867.04 ns | 431.22 ns | 9.584 |    0.03 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,512.1 ns |    25.08 ns |   1.37 ns | 0.142 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     167.8 ns |    17.74 ns |   0.97 ns | 0.009 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
