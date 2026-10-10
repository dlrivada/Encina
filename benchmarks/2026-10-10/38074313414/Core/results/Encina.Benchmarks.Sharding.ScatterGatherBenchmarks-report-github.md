```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **2,968.4 ns** |    **41.33 ns** |   **2.27 ns** |  **1.00** |    **0.00** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,035.4 ns |   675.69 ns |  37.04 ns |  1.02 |    0.01 |    3 | 0.0420 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,460.6 ns | 2,936.14 ns | 160.94 ns |  6.89 |    0.05 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,824.0 ns |    16.76 ns |   0.92 ns |  0.61 |    0.00 |    2 | 0.0267 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     133.3 ns |     3.19 ns |   0.17 ns |  0.04 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
|                                           |            |              |             |           |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **14,131.0 ns** |    **70.89 ns** |   **3.89 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,074.1 ns |   276.15 ns |  15.14 ns |  0.22 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 154,026.0 ns | 8,664.47 ns | 474.93 ns | 10.90 |    0.03 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,873.2 ns |   351.27 ns |  19.25 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     162.8 ns |    32.88 ns |   1.80 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
