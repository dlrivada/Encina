
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,395.4 ns** |    **284.86 ns** |  **15.61 ns** |  **1.00** |    **0.01** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,396.4 ns |    120.82 ns |   6.62 ns |  1.00 |    0.01 |    3 | 0.0420 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  15,879.0 ns |  1,323.07 ns |  72.52 ns |  6.63 |    0.05 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,444.1 ns |     76.78 ns |   4.21 ns |  0.60 |    0.00 |    2 | 0.0267 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     107.2 ns |      8.95 ns |   0.49 ns |  0.04 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
                                           |            |              |              |           |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **11,504.6 ns** |  **1,381.76 ns** |  **75.74 ns** |  **1.00** |    **0.01** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,408.0 ns |     58.64 ns |   3.21 ns |  0.21 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 128,306.5 ns | 12,684.90 ns | 695.30 ns | 11.15 |    0.08 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,449.7 ns |     25.88 ns |   1.42 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     146.5 ns |      9.65 ns |   0.53 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
