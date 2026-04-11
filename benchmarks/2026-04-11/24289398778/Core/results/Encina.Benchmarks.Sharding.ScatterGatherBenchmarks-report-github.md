```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | ShardCount | Mean         | Error       | StdDev      | Median       | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|------------:|------------:|-------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,375.0 ns** |   **103.19 ns** |   **148.00 ns** |   **3,470.4 ns** |   **1.00** |    **0.06** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,354.3 ns |    46.31 ns |    69.31 ns |   3,354.7 ns |   1.00 |    0.05 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,416.1 ns |   105.63 ns |   158.11 ns |  20,417.0 ns |   6.06 |    0.27 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,102.7 ns |    12.62 ns |    18.49 ns |   2,098.4 ns |   0.62 |    0.03 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     126.5 ns |     0.70 ns |     1.02 ns |     126.5 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |             |             |              |        |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **14,715.9 ns** |   **123.26 ns** |   **184.49 ns** |  **14,740.5 ns** |  **1.000** |    **0.02** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,443.8 ns |    54.42 ns |    79.77 ns |   3,432.4 ns |  0.234 |    0.01 |    3 |  0.2289 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 160,107.2 ns | 1,462.48 ns | 2,188.97 ns | 160,146.1 ns | 10.881 |    0.20 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,151.6 ns |    22.24 ns |    31.90 ns |   2,140.2 ns |  0.146 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     143.1 ns |     4.26 ns |     6.25 ns |     146.0 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
