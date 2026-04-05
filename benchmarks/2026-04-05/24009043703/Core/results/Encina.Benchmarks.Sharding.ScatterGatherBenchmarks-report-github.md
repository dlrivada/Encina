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
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          | **36,816.1 μs** |    **NA** |  **1.00** |    **4** |   **15984 B** |       **1.000** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          | 36,391.9 μs |    NA |  0.99 |    3 |    3704 B |       0.232 |
| &#39;Scatter-gather with large results&#39;       | 3          | 38,902.9 μs |    NA |  1.06 |    5 |   28704 B |       1.796 |
| &#39;Scatter-gather single shard&#39;             | 3          | 36,299.3 μs |    NA |  0.99 |    2 |    2328 B |       0.146 |
| &#39;Topology lookup all shards&#39;              | 3          |    989.3 μs |    NA |  0.03 |    1 |     104 B |       0.007 |
|                                           |            |             |       |       |      |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **10**         | **36,403.2 μs** |    **NA** |  **1.00** |    **3** |    **8904 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 10         | 36,316.7 μs |    NA |  1.00 |    2 |    3760 B |        0.42 |
| &#39;Scatter-gather with large results&#39;       | 10         | 37,581.0 μs |    NA |  1.03 |    5 |   99048 B |       11.12 |
| &#39;Scatter-gather single shard&#39;             | 10         | 36,416.4 μs |    NA |  1.00 |    4 |    2384 B |        0.27 |
| &#39;Topology lookup all shards&#39;              | 10         |  1,010.4 μs |    NA |  0.03 |    1 |     160 B |        0.02 |
|                                           |            |             |       |       |      |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         | **37,006.0 μs** |    **NA** |  **1.00** |    **4** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         | 36,460.5 μs |    NA |  0.99 |    3 |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 38,123.1 μs |    NA |  1.03 |    5 |  255288 B |       13.12 |
| &#39;Scatter-gather single shard&#39;             | 25         | 36,229.3 μs |    NA |  0.98 |    2 |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |    989.1 μs |    NA |  0.03 |    1 |     280 B |        0.01 |
