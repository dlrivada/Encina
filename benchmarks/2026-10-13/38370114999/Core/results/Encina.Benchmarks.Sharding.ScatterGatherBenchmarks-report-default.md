
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,441.1 ns** |    **411.15 ns** |    **22.54 ns** |  **1.00** |    **0.01** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,433.7 ns |    600.40 ns |    32.91 ns |  1.00 |    0.01 |    3 | 0.0420 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  17,089.3 ns |  4,769.01 ns |   261.41 ns |  7.00 |    0.11 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,469.2 ns |    431.99 ns |    23.68 ns |  0.60 |    0.01 |    2 | 0.0267 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     120.2 ns |     25.07 ns |     1.37 ns |  0.05 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
                                           |            |              |              |             |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **11,605.4 ns** |    **164.84 ns** |     **9.04 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,516.0 ns |  1,360.44 ns |    74.57 ns |  0.22 |    0.01 |    3 | 0.0458 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 131,172.4 ns | 44,843.99 ns | 2,458.05 ns | 11.30 |    0.18 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,494.7 ns |    664.73 ns |    36.44 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     137.5 ns |     49.11 ns |     2.69 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
