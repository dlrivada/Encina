
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|--------------:|-------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |  **2,161.11 ns** |    **363.980 ns** |    **19.951 ns** |  **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |  2,080.89 ns |    413.003 ns |    22.638 ns |  0.96 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 12,428.17 ns |  3,762.170 ns |   206.217 ns |  5.75 |    0.09 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |  1,313.41 ns |  1,148.261 ns |    62.940 ns |  0.61 |    0.03 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     74.95 ns |      6.010 ns |     0.329 ns |  0.03 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |               |              |       |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **9,390.11 ns** |  **6,511.698 ns** |   **356.928 ns** | **1.001** |    **0.05** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |  2,088.80 ns |    150.375 ns |     8.243 ns | 0.223 |    0.01 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 93,684.70 ns | 20,723.122 ns | 1,135.904 ns | 9.986 |    0.34 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |  1,311.69 ns |    236.703 ns |    12.975 ns | 0.140 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     88.71 ns |     18.642 ns |     1.022 ns | 0.009 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
