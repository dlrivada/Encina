```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev       | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|-------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |  **1,844.09 ns** |    **111.54 ns** |     **6.114 ns** |  **1.00** |    **0.00** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |  1,879.79 ns |  1,139.00 ns |    62.432 ns |  1.02 |    0.03 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          | 10,830.17 ns |    505.35 ns |    27.700 ns |  5.87 |    0.02 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |  1,180.71 ns |     82.41 ns |     4.517 ns |  0.64 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     70.37 ns |     45.51 ns |     2.494 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |              |              |       |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **8,384.35 ns** |  **5,237.63 ns** |   **287.092 ns** | **1.001** |    **0.04** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |  1,863.14 ns |    188.81 ns |    10.349 ns | 0.222 |    0.01 |    3 |  0.2308 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 82,151.91 ns | 24,547.23 ns | 1,345.517 ns | 9.806 |    0.32 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |  1,169.14 ns |    189.16 ns |    10.369 ns | 0.140 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     81.86 ns |     70.37 ns |     3.857 ns | 0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
