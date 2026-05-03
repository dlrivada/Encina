```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | ShardCount | Mean          | Error      | StdDev     | Median        | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------:|-----------:|-----------:|--------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **2,486.46 ns** |  **25.949 ns** |  **37.216 ns** |   **2,492.68 ns** |  **1.00** |    **0.02** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   2,463.86 ns |  11.042 ns |  16.185 ns |   2,463.20 ns |  0.99 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  16,826.80 ns |  87.707 ns | 131.276 ns |  16,790.77 ns |  6.77 |    0.11 |    4 |  1.7090 |  0.2136 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,588.30 ns |  15.620 ns |  23.379 ns |   1,574.00 ns |  0.64 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |      98.04 ns |   0.803 ns |   1.126 ns |      98.51 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |               |            |            |               |       |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **10,451.15 ns** |  **20.027 ns** |  **27.413 ns** |  **10,458.36 ns** |  **1.00** |    **0.00** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   2,589.56 ns |   4.583 ns |   6.425 ns |   2,589.22 ns |  0.25 |    0.00 |    3 |  0.2289 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 130,900.55 ns | 289.017 ns | 414.499 ns | 130,902.38 ns | 12.53 |    0.05 |    5 | 14.4043 | 10.4980 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,571.43 ns |   6.386 ns |   9.361 ns |   1,568.41 ns |  0.15 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     106.85 ns |   1.585 ns |   2.273 ns |     107.98 ns |  0.01 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
