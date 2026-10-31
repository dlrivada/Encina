
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev     | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|-----------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |  **1,886.34 ns** |    **410.19 ns** |  **22.484 ns** |   **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |  1,797.24 ns |     80.90 ns |   4.435 ns |   0.95 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 11,473.51 ns |  8,827.95 ns | 483.890 ns |   6.08 |    0.23 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |  1,156.67 ns |    297.40 ns |  16.302 ns |   0.61 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     72.05 ns |     93.33 ns |   5.116 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |            |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **8,706.08 ns** |  **1,376.34 ns** |  **75.442 ns** |  **1.000** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |  1,887.50 ns |    711.85 ns |  39.019 ns |  0.217 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 90,614.18 ns | 12,056.96 ns | 660.882 ns | 10.409 |    0.10 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |  1,224.12 ns |    193.95 ns |  10.631 ns |  0.141 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     84.91 ns |     27.59 ns |   1.512 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
