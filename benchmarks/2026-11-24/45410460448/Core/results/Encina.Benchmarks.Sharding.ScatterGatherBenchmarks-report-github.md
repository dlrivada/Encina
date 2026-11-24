```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **2,319.4 ns** |    **312.52 ns** |    **17.13 ns** |  **1.00** |    **0.01** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   2,356.7 ns |     89.88 ns |     4.93 ns |  1.02 |    0.01 |    3 | 0.0420 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  15,646.7 ns |    455.84 ns |    24.99 ns |  6.75 |    0.04 |    4 | 0.3357 | 0.0458 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,418.8 ns |     57.25 ns |     3.14 ns |  0.61 |    0.00 |    2 | 0.0267 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     103.2 ns |      5.76 ns |     0.32 ns |  0.04 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
|                                           |            |              |              |             |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **11,097.8 ns** |    **834.01 ns** |    **45.71 ns** |  **1.00** |    **0.01** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   2,369.3 ns |     73.55 ns |     4.03 ns |  0.21 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 127,713.9 ns | 21,828.91 ns | 1,196.52 ns | 11.51 |    0.10 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,428.8 ns |     44.77 ns |     2.45 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     126.4 ns |     85.04 ns |     4.66 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
