
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|----------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,600.0 ns** |    **451.31 ns** |  **24.74 ns** |   **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,639.3 ns |    898.40 ns |  49.24 ns |   1.02 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  18,456.6 ns |    399.63 ns |  21.91 ns |   7.10 |    0.06 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,646.4 ns |    173.81 ns |   9.53 ns |   0.63 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     101.6 ns |      7.81 ns |   0.43 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |           |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **11,258.1 ns** |    **837.08 ns** |  **45.88 ns** |  **1.000** |    **0.00** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,662.4 ns |    161.65 ns |   8.86 ns |  0.236 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 139,250.7 ns | 12,575.92 ns | 689.33 ns | 12.369 |    0.07 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,666.6 ns |    125.01 ns |   6.85 ns |  0.148 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     109.0 ns |     15.60 ns |   0.86 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
