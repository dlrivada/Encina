```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                    | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |------------:|------:|------:|-----:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          | **36,520.5 μs** |    **NA** |  **1.00** |    **4** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          | 35,785.2 μs |    NA |  0.98 |    2 |   16040 B |        4.40 |
| &#39;Scatter-gather with large results&#39;       | 3          | 37,369.7 μs |    NA |  1.02 |    5 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          | 36,295.6 μs |    NA |  0.99 |    3 |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |  1,022.0 μs |    NA |  0.03 |    1 |     104 B |        0.03 |
|                                           |            |             |       |       |      |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **10**         | **36,307.5 μs** |    **NA** |  **1.00** |    **4** |    **8904 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 10         | 35,856.6 μs |    NA |  0.99 |    2 |    3760 B |        0.42 |
| &#39;Scatter-gather with large results&#39;       | 10         | 37,285.7 μs |    NA |  1.03 |    5 |   99048 B |       11.12 |
| &#39;Scatter-gather single shard&#39;             | 10         | 35,933.9 μs |    NA |  0.99 |    3 |    2384 B |        0.27 |
| &#39;Topology lookup all shards&#39;              | 10         |    977.0 μs |    NA |  0.03 |    1 |     160 B |        0.02 |
|                                           |            |             |       |       |      |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         | **36,822.3 μs** |    **NA** |  **1.00** |    **3** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         | 36,039.2 μs |    NA |  0.98 |    2 |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 38,131.0 μs |    NA |  1.04 |    5 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         | 36,910.2 μs |    NA |  1.00 |    4 |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |  1,003.1 μs |    NA |  0.03 |    1 |     280 B |        0.01 |
