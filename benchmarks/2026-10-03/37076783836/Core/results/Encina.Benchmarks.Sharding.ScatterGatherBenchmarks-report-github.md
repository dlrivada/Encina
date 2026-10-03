```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean          | Error        | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------:|-------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **2,060.32 ns** |    **208.48 ns** |    **11.428 ns** |  **1.00** |    **0.01** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   2,158.96 ns |    296.70 ns |    16.263 ns |  1.05 |    0.01 |    3 | 0.0420 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  14,223.08 ns |  2,011.63 ns |   110.264 ns |  6.90 |    0.06 |    4 | 0.3357 | 0.0458 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,278.28 ns |    174.32 ns |     9.555 ns |  0.62 |    0.00 |    2 | 0.0267 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |      96.57 ns |     46.32 ns |     2.539 ns |  0.05 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
|                                           |            |               |              |              |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **10,660.74 ns** |  **4,534.20 ns** |   **248.535 ns** |  **1.00** |    **0.03** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   2,299.22 ns |    693.37 ns |    38.006 ns |  0.22 |    0.01 |    3 | 0.0458 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 110,258.49 ns | 33,650.63 ns | 1,844.505 ns | 10.35 |    0.26 |    5 | 2.8076 | 1.8311 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,397.60 ns |  1,077.22 ns |    59.046 ns |  0.13 |    0.01 |    2 | 0.0286 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     122.25 ns |     74.41 ns |     4.079 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
