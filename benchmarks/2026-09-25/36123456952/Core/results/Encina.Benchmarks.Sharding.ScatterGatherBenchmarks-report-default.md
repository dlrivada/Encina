
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,467.9 ns** |    **158.56 ns** |   **8.69 ns** |  **1.00** |    **0.00** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,533.3 ns |    324.01 ns |  17.76 ns |  1.02 |    0.00 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  20,652.8 ns |  2,683.49 ns | 147.09 ns |  5.96 |    0.04 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,177.5 ns |    243.23 ns |  13.33 ns |  0.63 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     132.1 ns |     19.33 ns |   1.06 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |           |       |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **14,584.2 ns** |  **2,021.70 ns** | **110.82 ns** |  **1.00** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,591.2 ns |    232.07 ns |  12.72 ns |  0.25 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 160,435.7 ns | 16,529.60 ns | 906.04 ns | 11.00 |    0.09 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,334.3 ns |     93.92 ns |   5.15 ns |  0.16 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     147.1 ns |      8.01 ns |   0.44 ns |  0.01 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
