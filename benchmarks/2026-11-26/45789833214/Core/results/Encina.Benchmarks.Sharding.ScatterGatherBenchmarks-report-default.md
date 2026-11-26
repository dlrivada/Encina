
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean          | Error        | StdDev       | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |--------------:|-------------:|-------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,443.79 ns** |    **144.59 ns** |     **7.925 ns** |  **1.00** |    **0.00** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,452.38 ns |    222.34 ns |    12.187 ns |  1.00 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  17,260.16 ns |  2,134.20 ns |   116.983 ns |  7.06 |    0.05 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,569.82 ns |    198.99 ns |    10.907 ns |  0.64 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |      99.22 ns |     14.07 ns |     0.771 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |               |              |              |       |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **10,619.40 ns** |  **1,753.38 ns** |    **96.109 ns** |  **1.00** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,517.80 ns |    369.59 ns |    20.259 ns |  0.24 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 136,665.06 ns | 26,572.19 ns | 1,456.511 ns | 12.87 |    0.16 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,592.78 ns |    114.37 ns |     6.269 ns |  0.15 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     108.37 ns |     14.37 ns |     0.788 ns |  0.01 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
