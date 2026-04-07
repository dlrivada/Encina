```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                    | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |------------:|------:|------:|-----:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          | **36,465.0 μs** |    **NA** |  **1.00** |    **3** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          | 36,219.0 μs |    NA |  0.99 |    2 |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          | 38,680.6 μs |    NA |  1.06 |    5 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          | 36,509.7 μs |    NA |  1.00 |    4 |   14664 B |        4.02 |
| &#39;Topology lookup all shards&#39;              | 3          |  1,024.0 μs |    NA |  0.03 |    1 |     104 B |        0.03 |
|                                           |            |             |       |       |      |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         | **36,887.5 μs** |    **NA** |  **1.00** |    **4** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         | 36,281.2 μs |    NA |  0.98 |    2 |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 37,925.6 μs |    NA |  1.03 |    5 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         | 36,868.3 μs |    NA |  1.00 |    3 |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |    985.5 μs |    NA |  0.03 |    1 |     280 B |        0.01 |
