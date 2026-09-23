
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,618.2 ns** |    **192.44 ns** |    **10.55 ns** |   **1.00** |    **0.00** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,511.6 ns |  1,023.39 ns |    56.10 ns |   0.96 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  17,749.5 ns |  2,058.63 ns |   112.84 ns |   6.78 |    0.04 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,647.6 ns |     81.63 ns |     4.47 ns |   0.63 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     100.1 ns |     13.21 ns |     0.72 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |             |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **10,970.2 ns** |  **1,601.54 ns** |    **87.79 ns** |  **1.000** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,622.7 ns |    233.02 ns |    12.77 ns |  0.239 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 136,472.9 ns | 29,922.45 ns | 1,640.15 ns | 12.441 |    0.16 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,668.7 ns |    703.39 ns |    38.56 ns |  0.152 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     106.9 ns |      5.30 ns |     0.29 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
