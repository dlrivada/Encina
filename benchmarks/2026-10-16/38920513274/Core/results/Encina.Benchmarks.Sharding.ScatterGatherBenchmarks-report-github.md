```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,885.2 ns** |   **388.27 ns** |  **21.28 ns** |  **1.00** |    **0.01** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,695.1 ns |   277.34 ns |  15.20 ns |  0.95 |    0.01 |    3 | 0.1450 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  21,573.9 ns |   327.95 ns |  17.98 ns |  5.55 |    0.03 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,414.1 ns |    55.02 ns |   3.02 ns |  0.62 |    0.00 |    2 | 0.0916 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     129.0 ns |     5.07 ns |   0.28 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
|                                           |            |              |             |           |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **17,036.7 ns** |   **525.20 ns** |  **28.79 ns** | **1.000** |    **0.00** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,792.7 ns |   114.76 ns |   6.29 ns | 0.223 |    0.00 |    3 | 0.1526 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 168,135.4 ns | 3,801.10 ns | 208.35 ns | 9.869 |    0.02 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,440.7 ns |    68.91 ns |   3.78 ns | 0.143 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     157.3 ns |     6.64 ns |   0.36 ns | 0.009 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
