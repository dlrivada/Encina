```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **2,278.3 ns** |    **385.38 ns** |  **21.12 ns** |  **1.00** |    **0.01** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   2,300.1 ns |     44.03 ns |   2.41 ns |  1.01 |    0.01 |    3 | 0.0420 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  15,240.8 ns |  2,597.22 ns | 142.36 ns |  6.69 |    0.08 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,456.6 ns |     89.12 ns |   4.88 ns |  0.64 |    0.01 |    2 | 0.0267 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     100.3 ns |      8.38 ns |   0.46 ns |  0.04 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
|                                           |            |              |              |           |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **11,014.7 ns** |    **777.03 ns** |  **42.59 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   2,304.8 ns |    307.19 ns |  16.84 ns |  0.21 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 121,562.3 ns | 11,762.22 ns | 644.73 ns | 11.04 |    0.06 |    5 | 2.8076 | 1.8311 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,442.0 ns |    168.31 ns |   9.23 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     131.0 ns |     36.32 ns |   1.99 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
