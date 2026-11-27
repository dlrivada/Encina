
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,363.7 ns** |   **222.29 ns** |  **12.18 ns** |  **1.00** |    **0.01** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,376.2 ns |    49.21 ns |   2.70 ns |  1.01 |    0.00 |    3 | 0.0420 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  15,941.9 ns |   472.37 ns |  25.89 ns |  6.74 |    0.03 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,437.2 ns |    71.63 ns |   3.93 ns |  0.61 |    0.00 |    2 | 0.0267 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     101.9 ns |     7.86 ns |   0.43 ns |  0.04 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
                                           |            |              |             |           |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **11,407.0 ns** |   **328.40 ns** |  **18.00 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,403.6 ns |    92.26 ns |   5.06 ns |  0.21 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 124,124.0 ns | 8,092.31 ns | 443.57 ns | 10.88 |    0.04 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,478.0 ns |    13.57 ns |   0.74 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     139.7 ns |    24.14 ns |   1.32 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
