```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|--------------:|-------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |  **1,902.49 ns** |    **308.383 ns** |    **16.904 ns** |  **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |  1,817.39 ns |     21.789 ns |     1.194 ns |  0.96 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          | 10,799.24 ns |  5,619.756 ns |   308.038 ns |  5.68 |    0.15 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |  1,161.66 ns |    315.307 ns |    17.283 ns |  0.61 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     68.00 ns |      9.387 ns |     0.515 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |               |              |       |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **8,291.62 ns** |  **2,004.471 ns** |   **109.872 ns** | **1.000** |    **0.02** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |  1,864.88 ns |    922.787 ns |    50.581 ns | 0.225 |    0.01 |    3 |  0.2308 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 82,737.52 ns | 32,876.509 ns | 1,802.072 ns | 9.980 |    0.22 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |  1,177.00 ns |    117.672 ns |     6.450 ns | 0.142 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     80.69 ns |     44.490 ns |     2.439 ns | 0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
