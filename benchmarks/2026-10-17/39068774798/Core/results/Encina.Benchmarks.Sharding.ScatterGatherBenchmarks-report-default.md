
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error         | StdDev       | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|--------------:|-------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |  **1,916.68 ns** |    **983.277 ns** |    **53.897 ns** |   **1.00** |    **0.03** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |  1,856.91 ns |    336.066 ns |    18.421 ns |   0.97 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 11,390.58 ns |    986.561 ns |    54.077 ns |   5.95 |    0.15 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |  1,195.29 ns |     87.045 ns |     4.771 ns |   0.62 |    0.02 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     71.60 ns |      7.224 ns |     0.396 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |               |              |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **8,136.82 ns** |     **53.870 ns** |     **2.953 ns** |  **1.000** |    **0.00** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |  1,892.40 ns |    160.570 ns |     8.801 ns |  0.233 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 86,037.48 ns | 56,808.987 ns | 3,113.892 ns | 10.574 |    0.33 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |  1,214.36 ns |    544.641 ns |    29.854 ns |  0.149 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     80.74 ns |      3.874 ns |     0.212 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
