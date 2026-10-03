```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|----------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,385.1 ns** |    **197.87 ns** |  **10.85 ns** |   **1.00** |    **0.00** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,489.5 ns |    462.64 ns |  25.36 ns |   1.03 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,289.4 ns |  3,445.14 ns | 188.84 ns |   5.99 |    0.05 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,179.4 ns |    351.77 ns |  19.28 ns |   0.64 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     133.6 ns |      9.59 ns |   0.53 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |              |           |        |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **14,271.7 ns** |    **635.13 ns** |  **34.81 ns** |  **1.000** |    **0.00** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,503.1 ns |    200.34 ns |  10.98 ns |  0.245 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 157,185.3 ns | 16,623.03 ns | 911.16 ns | 11.014 |    0.06 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,173.0 ns |    183.01 ns |  10.03 ns |  0.152 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     142.1 ns |     13.95 ns |   0.76 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
