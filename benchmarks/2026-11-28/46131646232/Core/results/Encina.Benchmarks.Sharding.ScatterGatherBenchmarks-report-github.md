```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,020.7 ns** |     **51.80 ns** |   **2.84 ns** |  **1.00** |    **0.00** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,096.2 ns |    150.78 ns |   8.26 ns |  1.02 |    0.00 |    3 | 0.0420 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,805.8 ns |  1,252.45 ns |  68.65 ns |  6.89 |    0.02 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,880.4 ns |    257.15 ns |  14.10 ns |  0.62 |    0.00 |    2 | 0.0267 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     134.6 ns |      6.73 ns |   0.37 ns |  0.04 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
|                                           |            |              |              |           |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **14,481.2 ns** |    **234.62 ns** |  **12.86 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,098.8 ns |    247.94 ns |  13.59 ns |  0.21 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 159,554.8 ns | 11,037.18 ns | 604.99 ns | 11.02 |    0.04 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,893.1 ns |    206.22 ns |  11.30 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     165.6 ns |     19.64 ns |   1.08 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
