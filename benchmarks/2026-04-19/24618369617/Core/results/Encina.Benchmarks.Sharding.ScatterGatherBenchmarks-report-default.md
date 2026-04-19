
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                                    | ShardCount | Mean         | Error       | StdDev      | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,365.6 ns** |    **30.39 ns** |    **44.54 ns** |   **1.00** |    **0.02** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,328.9 ns |    23.66 ns |    35.42 ns |   0.99 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  20,398.4 ns |    64.01 ns |    93.83 ns |   6.06 |    0.08 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,145.3 ns |    17.53 ns |    26.24 ns |   0.64 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     131.3 ns |     0.49 ns |     0.72 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |             |             |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **14,471.2 ns** |    **56.59 ns** |    **82.94 ns** |  **1.000** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,405.3 ns |    19.82 ns |    29.66 ns |  0.235 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 160,940.2 ns | 1,415.75 ns | 2,119.02 ns | 11.122 |    0.16 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,131.8 ns |     8.98 ns |    12.88 ns |  0.147 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     142.3 ns |     1.52 ns |     2.23 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
