
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev       | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|-------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |  **2,080.18 ns** |    **320.74 ns** |    **17.581 ns** |   **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |  2,050.32 ns |    845.80 ns |    46.361 ns |   0.99 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 12,334.20 ns |    330.46 ns |    18.114 ns |   5.93 |    0.04 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |  1,278.65 ns |    172.99 ns |     9.482 ns |   0.61 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     72.76 ns |     41.40 ns |     2.269 ns |   0.03 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |              |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **8,974.26 ns** |  **4,703.41 ns** |   **257.810 ns** |  **1.001** |    **0.03** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |  2,101.23 ns |    868.81 ns |    47.623 ns |  0.234 |    0.01 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 96,345.96 ns | 43,260.37 ns | 2,371.247 ns | 10.742 |    0.35 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |  1,346.40 ns |  1,363.91 ns |    74.761 ns |  0.150 |    0.01 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     89.45 ns |     32.90 ns |     1.803 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
