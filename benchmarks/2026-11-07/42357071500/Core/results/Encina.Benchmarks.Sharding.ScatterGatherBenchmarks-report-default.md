
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|----------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,202.1 ns** |   **684.08 ns** |  **37.50 ns** |   **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,270.0 ns | 1,211.36 ns |  66.40 ns |   1.02 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  22,547.6 ns | 1,791.48 ns |  98.20 ns |   7.04 |    0.08 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,097.1 ns |   384.73 ns |  21.09 ns |   0.65 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     132.8 ns |    28.34 ns |   1.55 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |             |           |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **14,403.1 ns** | **4,745.70 ns** | **260.13 ns** |  **1.000** |    **0.02** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,353.4 ns |   411.46 ns |  22.55 ns |  0.233 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 179,213.2 ns | 8,124.40 ns | 445.33 ns | 12.445 |    0.20 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,114.6 ns |   636.15 ns |  34.87 ns |  0.147 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     141.2 ns |    17.67 ns |   0.97 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
