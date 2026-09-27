```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,797.0 ns** |   **138.09 ns** |   **7.57 ns** |  **1.00** |    **0.00** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,793.4 ns |    62.51 ns |   3.43 ns |  1.00 |    0.00 |    3 | 0.1450 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  21,181.0 ns |   446.00 ns |  24.45 ns |  5.58 |    0.01 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,454.7 ns |    57.44 ns |   3.15 ns |  0.65 |    0.00 |    2 | 0.0916 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     126.9 ns |    12.65 ns |   0.69 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
|                                           |            |              |             |           |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **16,460.7 ns** |   **672.06 ns** |  **36.84 ns** | **1.000** |    **0.00** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,940.6 ns |   123.73 ns |   6.78 ns | 0.239 |    0.00 |    3 | 0.1526 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 162,342.2 ns | 6,294.64 ns | 345.03 ns | 9.862 |    0.03 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,469.5 ns |    16.57 ns |   0.91 ns | 0.150 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     164.0 ns |    34.75 ns |   1.90 ns | 0.010 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
