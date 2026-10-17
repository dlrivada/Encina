```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **2,920.2 ns** |    **199.81 ns** |    **10.95 ns** |  **1.00** |    **0.00** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   2,960.7 ns |    186.73 ns |    10.24 ns |  1.01 |    0.00 |    3 | 0.0420 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  20,523.3 ns |  9,220.41 ns |   505.40 ns |  7.03 |    0.15 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   1,902.2 ns |    208.78 ns |    11.44 ns |  0.65 |    0.00 |    2 | 0.0267 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     138.0 ns |      8.19 ns |     0.45 ns |  0.05 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
|                                           |            |              |              |             |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **14,159.9 ns** |  **1,863.22 ns** |   **102.13 ns** |  **1.00** |    **0.01** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,229.5 ns |  1,513.80 ns |    82.98 ns |  0.23 |    0.01 |    3 | 0.0458 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 170,020.8 ns | 28,907.68 ns | 1,584.53 ns | 12.01 |    0.12 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   1,898.9 ns |    246.71 ns |    13.52 ns |  0.13 |    0.00 |    2 | 0.0267 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     158.1 ns |     46.61 ns |     2.55 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
