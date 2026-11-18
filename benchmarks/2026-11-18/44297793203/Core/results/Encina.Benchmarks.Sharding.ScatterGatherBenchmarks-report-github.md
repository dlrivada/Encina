```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|------------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |  **1,737.23 ns** |   **716.78 ns** |  **39.289 ns** |  **1.00** |    **0.03** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |  1,838.21 ns |   914.04 ns |  50.101 ns |  1.06 |    0.03 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          | 10,398.78 ns | 2,049.58 ns | 112.345 ns |  5.99 |    0.13 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |  1,127.39 ns |   314.26 ns |  17.226 ns |  0.65 |    0.02 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     69.85 ns |    12.19 ns |   0.668 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |             |            |       |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **8,093.87 ns** | **2,058.21 ns** | **112.817 ns** |  **1.00** |    **0.02** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |  1,868.36 ns |   805.48 ns |  44.151 ns |  0.23 |    0.01 |    3 |  0.2308 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 83,702.42 ns | 6,312.37 ns | 346.002 ns | 10.34 |    0.13 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |  1,204.03 ns |   925.66 ns |  50.739 ns |  0.15 |    0.01 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     82.46 ns |    28.18 ns |   1.545 ns |  0.01 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
