```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,266.0 ns** |    **166.20 ns** |   **9.11 ns** |  **1.00** |    **0.00** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,326.5 ns |    209.01 ns |  11.46 ns |  1.02 |    0.00 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,370.9 ns |  5,929.00 ns | 324.99 ns |  6.24 |    0.09 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,115.3 ns |     96.28 ns |   5.28 ns |  0.65 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     127.7 ns |     12.44 ns |   0.68 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |              |           |       |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **10**         |   **7,152.2 ns** |    **263.39 ns** |  **14.44 ns** |  **1.00** |    **0.00** |    **4** |  **0.5264** |  **0.0076** |    **8904 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 10         |   3,349.4 ns |    186.17 ns |  10.20 ns |  0.47 |    0.00 |    3 |  0.2213 |       - |    3760 B |        0.42 |
| &#39;Scatter-gather with large results&#39;       | 10         |  63,169.6 ns | 10,883.47 ns | 596.56 ns |  8.83 |    0.07 |    5 |  5.8594 |  2.6855 |   99048 B |       11.12 |
| &#39;Scatter-gather single shard&#39;             | 10         |   2,065.0 ns |     23.12 ns |   1.27 ns |  0.29 |    0.00 |    2 |  0.1411 |       - |    2384 B |        0.27 |
| &#39;Topology lookup all shards&#39;              | 10         |     132.9 ns |      0.38 ns |   0.02 ns |  0.02 |    0.00 |    1 |  0.0095 |       - |     160 B |        0.02 |
|                                           |            |              |              |           |       |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **14,017.9 ns** |  **1,223.22 ns** |  **67.05 ns** |  **1.00** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,385.0 ns |    193.14 ns |  10.59 ns |  0.24 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 155,001.7 ns | 10,316.66 ns | 565.49 ns | 11.06 |    0.06 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,140.9 ns |    106.76 ns |   5.85 ns |  0.15 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     145.3 ns |      4.34 ns |   0.24 ns |  0.01 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
