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
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,368.8 ns** |    **34.53 ns** |    **51.69 ns** |   **3,366.8 ns** |   **1.00** |    **0.02** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,433.9 ns |    26.33 ns |    39.40 ns |   3,431.4 ns |   1.02 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,899.2 ns |   129.50 ns |   193.82 ns |  20,898.6 ns |   6.21 |    0.11 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,209.5 ns |    16.01 ns |    23.97 ns |   2,203.4 ns |   0.66 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     129.9 ns |     0.45 ns |     0.66 ns |     129.7 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |             |             |              |        |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **14,778.1 ns** |   **100.26 ns** |   **146.96 ns** |  **14,762.6 ns** |  **1.000** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,442.6 ns |    18.66 ns |    27.35 ns |   3,446.7 ns |  0.233 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 161,692.4 ns | 1,566.23 ns | 2,344.27 ns | 161,777.8 ns | 10.942 |    0.19 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,143.5 ns |    12.69 ns |    18.60 ns |   2,146.7 ns |  0.145 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     147.5 ns |     3.80 ns |     5.57 ns |     144.3 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
