```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **2,242.51 ns** |   **1,916.31 ns** |   **105.039 ns** |  **1.00** |    **0.06** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   2,163.94 ns |     132.76 ns |     7.277 ns |  0.97 |    0.04 |    3 | 0.0420 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  15,454.49 ns |   1,376.55 ns |    75.453 ns |  6.90 |    0.28 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,417.15 ns |   1,522.49 ns |    83.453 ns |  0.63 |    0.04 |    2 | 0.0267 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |      96.04 ns |      66.84 ns |     3.664 ns |  0.04 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
|                                           |            |               |               |              |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **10,648.95 ns** |     **268.12 ns** |    **14.696 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   2,234.46 ns |      57.15 ns |     3.133 ns |  0.21 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 127,155.67 ns | 147,039.08 ns | 8,059.708 ns | 11.94 |    0.66 |    5 | 2.8076 | 1.8311 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,344.61 ns |      77.21 ns |     4.232 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     130.33 ns |      65.10 ns |     3.568 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
