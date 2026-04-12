```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.64GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | ShardCount | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **4,047.3 ns** |    **14.15 ns** |    **20.30 ns** |  **1.00** |    **0.01** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,934.2 ns |    32.57 ns |    46.72 ns |  0.97 |    0.01 |    3 | 0.1450 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,784.8 ns |   146.69 ns |   215.01 ns |  5.14 |    0.06 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,438.2 ns |    31.55 ns |    47.22 ns |  0.60 |    0.01 |    2 | 0.0916 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     139.8 ns |     1.62 ns |     2.42 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
|                                           |            |              |             |             |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **17,155.4 ns** |   **224.40 ns** |   **335.87 ns** |  **1.00** |    **0.03** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   4,142.1 ns |   108.22 ns |   161.98 ns |  0.24 |    0.01 |    3 | 0.1526 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 171,746.2 ns | 1,141.75 ns | 1,708.92 ns | 10.01 |    0.21 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,503.2 ns |    12.96 ns |    19.40 ns |  0.15 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     176.5 ns |     1.70 ns |     2.54 ns |  0.01 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
