```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,358.3 ns** |    **497.81 ns** |    **27.29 ns** |   **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,485.6 ns |    374.90 ns |    20.55 ns |   1.04 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,834.0 ns |  1,919.06 ns |   105.19 ns |   6.20 |    0.05 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,220.5 ns |     32.30 ns |     1.77 ns |   0.66 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     134.7 ns |      2.47 ns |     0.14 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |              |             |        |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **15,174.7 ns** |    **546.00 ns** |    **29.93 ns** |  **1.000** |    **0.00** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,457.0 ns |    323.58 ns |    17.74 ns |  0.228 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 159,497.1 ns | 20,207.54 ns | 1,107.64 ns | 10.511 |    0.07 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,156.4 ns |    112.48 ns |     6.17 ns |  0.142 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     142.3 ns |     19.88 ns |     1.09 ns |  0.009 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
