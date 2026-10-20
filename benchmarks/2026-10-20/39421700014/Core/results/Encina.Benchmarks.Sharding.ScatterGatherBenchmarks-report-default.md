
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|----------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,541.7 ns** |    **84.23 ns** |   **4.62 ns** |   **1.00** |    **0.00** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,568.3 ns |   451.31 ns |  24.74 ns |   1.01 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  21,093.2 ns | 8,607.30 ns | 471.80 ns |   5.96 |    0.12 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,255.2 ns |    72.58 ns |   3.98 ns |   0.64 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     136.3 ns |    89.21 ns |   4.89 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |             |           |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **15,121.5 ns** |   **551.17 ns** |  **30.21 ns** |  **1.000** |    **0.00** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,712.8 ns |   104.88 ns |   5.75 ns |  0.246 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 166,650.9 ns | 6,567.61 ns | 359.99 ns | 11.021 |    0.03 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,236.2 ns |     8.54 ns |   0.47 ns |  0.148 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     144.3 ns |    16.99 ns |   0.93 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
