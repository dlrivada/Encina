
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                                    | ShardCount | Mean         | Error     | StdDev      | Median       | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|----------:|------------:|-------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,530.3 ns** |  **26.89 ns** |    **38.56 ns** |   **3,510.6 ns** |   **1.00** |    **0.02** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,554.3 ns |  14.82 ns |    21.72 ns |   3,556.3 ns |   1.01 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  21,193.7 ns |  51.26 ns |    73.52 ns |  21,168.1 ns |   6.00 |    0.07 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,274.7 ns |  30.15 ns |    43.24 ns |   2,306.0 ns |   0.64 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     136.3 ns |   1.77 ns |     2.65 ns |     136.4 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |           |             |              |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **14,976.3 ns** |  **60.65 ns** |    **90.78 ns** |  **14,962.1 ns** |  **1.000** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,541.0 ns |  18.28 ns |    25.62 ns |   3,531.5 ns |  0.236 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 164,948.6 ns | 834.54 ns | 1,196.88 ns | 165,431.7 ns | 11.014 |    0.10 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,286.8 ns |   9.93 ns |    14.24 ns |   2,281.7 ns |  0.153 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     146.1 ns |   2.16 ns |     3.17 ns |     147.5 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
