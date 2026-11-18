```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,084.1 ns** |    **212.55 ns** |    **11.65 ns** |  **1.00** |    **0.00** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,064.3 ns |    184.70 ns |    10.12 ns |  0.99 |    0.00 |    3 | 0.0420 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,704.7 ns |    525.73 ns |    28.82 ns |  6.71 |    0.02 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,881.6 ns |     82.84 ns |     4.54 ns |  0.61 |    0.00 |    2 | 0.0267 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     136.3 ns |     24.85 ns |     1.36 ns |  0.04 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
|                                           |            |              |              |             |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **14,344.2 ns** |    **415.83 ns** |    **22.79 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,051.2 ns |     38.46 ns |     2.11 ns |  0.21 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 157,374.6 ns | 25,814.25 ns | 1,414.97 ns | 10.97 |    0.09 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,870.9 ns |    246.50 ns |    13.51 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     164.9 ns |     17.87 ns |     0.98 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
