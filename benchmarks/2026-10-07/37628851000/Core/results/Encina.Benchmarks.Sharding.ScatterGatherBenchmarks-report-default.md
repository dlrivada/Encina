
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|----------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,446.7 ns** |    **259.96 ns** |  **14.25 ns** |   **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,584.9 ns |    235.86 ns |  12.93 ns |   1.04 |    0.00 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  20,561.1 ns |  1,153.33 ns |  63.22 ns |   5.97 |    0.03 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,180.4 ns |    253.29 ns |  13.88 ns |   0.63 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     132.6 ns |      0.42 ns |   0.02 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |           |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **14,812.8 ns** |  **4,739.40 ns** | **259.78 ns** |  **1.000** |    **0.02** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,460.3 ns |    237.24 ns |  13.00 ns |  0.234 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 157,212.6 ns | 15,484.27 ns | 848.75 ns | 10.615 |    0.17 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,179.1 ns |    127.43 ns |   6.98 ns |  0.147 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     144.0 ns |      6.05 ns |   0.33 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
