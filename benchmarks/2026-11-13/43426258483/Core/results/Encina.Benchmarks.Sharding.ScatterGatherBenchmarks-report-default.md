
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,488.9 ns** |    **943.88 ns** |    **51.74 ns** |   **1.00** |    **0.03** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,468.7 ns |     62.30 ns |     3.42 ns |   0.99 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  17,735.8 ns |  4,835.25 ns |   265.04 ns |   7.13 |    0.16 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,648.9 ns |  1,041.78 ns |    57.10 ns |   0.66 |    0.02 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     101.7 ns |     11.03 ns |     0.60 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |             |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **10,914.6 ns** |  **2,126.41 ns** |   **116.56 ns** |  **1.000** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,625.0 ns |  1,937.17 ns |   106.18 ns |  0.241 |    0.01 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 141,083.8 ns | 59,899.99 ns | 3,283.32 ns | 12.927 |    0.29 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,639.3 ns |    255.63 ns |    14.01 ns |  0.150 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     107.3 ns |     16.21 ns |     0.89 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
