```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,844.8 ns** |    **190.57 ns** |    **10.45 ns** |  **1.00** |    **0.00** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,815.9 ns |    301.14 ns |    16.51 ns |  0.99 |    0.00 |    3 | 0.1450 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,310.4 ns |  1,811.97 ns |    99.32 ns |  5.28 |    0.03 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,416.3 ns |    112.05 ns |     6.14 ns |  0.63 |    0.00 |    2 | 0.0916 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     124.1 ns |      4.44 ns |     0.24 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
|                                           |            |              |              |             |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **16,016.0 ns** |  **1,561.79 ns** |    **85.61 ns** | **1.000** |    **0.01** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,718.6 ns |    327.24 ns |    17.94 ns | 0.232 |    0.00 |    3 | 0.1526 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 156,080.1 ns | 42,260.68 ns | 2,316.45 ns | 9.745 |    0.13 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,443.4 ns |     86.11 ns |     4.72 ns | 0.153 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     147.4 ns |     16.30 ns |     0.89 ns | 0.009 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
