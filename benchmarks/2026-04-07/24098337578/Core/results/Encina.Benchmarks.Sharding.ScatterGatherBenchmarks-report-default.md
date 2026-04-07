
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                                    | ShardCount | Mean         | Error     | StdDev      | Median       | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|----------:|------------:|-------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,143.7 ns** |  **15.49 ns** |    **23.19 ns** |   **3,156.3 ns** |  **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,231.6 ns |  37.30 ns |    55.83 ns |   3,236.6 ns |  1.03 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  22,323.4 ns | 427.23 ns |   639.45 ns |  22,317.5 ns |  7.10 |    0.21 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,017.4 ns |  18.28 ns |    25.02 ns |   2,015.7 ns |  0.64 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     127.6 ns |   0.73 ns |     1.05 ns |     128.0 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |           |             |              |       |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **13,465.1 ns** |  **32.68 ns** |    **46.87 ns** |  **13,477.4 ns** |  **1.00** |    **0.00** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,261.0 ns |  16.93 ns |    24.82 ns |   3,258.0 ns |  0.24 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 168,164.8 ns | 891.56 ns | 1,306.83 ns | 168,035.5 ns | 12.49 |    0.10 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,031.6 ns |   5.90 ns |     8.46 ns |   2,035.1 ns |  0.15 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     136.1 ns |   0.43 ns |     0.59 ns |     136.3 ns |  0.01 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
