
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|--------------:|-------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |  **1,896.39 ns** |    **446.752 ns** |    **24.488 ns** |  **1.00** |    **0.02** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |  1,853.22 ns |    515.231 ns |    28.242 ns |  0.98 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 10,595.54 ns |  5,749.086 ns |   315.127 ns |  5.59 |    0.16 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |  1,205.62 ns |     26.796 ns |     1.469 ns |  0.64 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     65.86 ns |      9.943 ns |     0.545 ns |  0.03 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |               |              |       |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **8,437.90 ns** |  **3,529.389 ns** |   **193.458 ns** | **1.000** |    **0.03** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |  1,843.01 ns |    486.700 ns |    26.678 ns | 0.218 |    0.01 |    3 |  0.2308 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 81,759.49 ns | 19,225.211 ns | 1,053.799 ns | 9.693 |    0.22 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |  1,162.01 ns |    103.059 ns |     5.649 ns | 0.138 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     75.03 ns |     17.445 ns |     0.956 ns | 0.009 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
