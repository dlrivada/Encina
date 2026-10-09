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
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **2,988.4 ns** |   **178.03 ns** |   **9.76 ns** |  **1.00** |    **0.00** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,054.7 ns |   570.30 ns |  31.26 ns |  1.02 |    0.01 |    3 | 0.0420 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,548.5 ns |   797.50 ns |  43.71 ns |  6.88 |    0.02 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,884.2 ns |   318.31 ns |  17.45 ns |  0.63 |    0.01 |    2 | 0.0267 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     136.9 ns |     7.27 ns |   0.40 ns |  0.05 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
|                                           |            |              |             |           |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **14,386.3 ns** |   **350.73 ns** |  **19.22 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,150.8 ns |   618.30 ns |  33.89 ns |  0.22 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 156,397.4 ns | 8,464.82 ns | 463.99 ns | 10.87 |    0.03 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,944.0 ns |   261.02 ns |  14.31 ns |  0.14 |    0.00 |    2 | 0.0267 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     186.0 ns |    38.95 ns |   2.14 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
