```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | ShardCount | Mean         | Error       | StdDev      | Median       | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|------------:|------------:|-------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,156.2 ns** |    **12.99 ns** |    **18.64 ns** |   **3,157.0 ns** |  **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,225.9 ns |    16.29 ns |    22.83 ns |   3,229.2 ns |  1.02 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  21,410.8 ns |   118.82 ns |   174.16 ns |  21,389.4 ns |  6.78 |    0.07 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,010.1 ns |    13.31 ns |    18.22 ns |   2,008.9 ns |  0.64 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     127.5 ns |     0.62 ns |     0.88 ns |     127.6 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |             |             |              |       |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **13,714.6 ns** |   **105.91 ns** |   **155.24 ns** |  **13,620.9 ns** |  **1.00** |    **0.02** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,163.9 ns |    18.62 ns |    25.49 ns |   3,163.6 ns |  0.23 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 169,547.1 ns | 1,665.77 ns | 2,441.66 ns | 168,596.1 ns | 12.36 |    0.22 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,035.0 ns |    26.92 ns |    38.60 ns |   2,025.1 ns |  0.15 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     139.6 ns |     3.12 ns |     4.57 ns |     142.5 ns |  0.01 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
