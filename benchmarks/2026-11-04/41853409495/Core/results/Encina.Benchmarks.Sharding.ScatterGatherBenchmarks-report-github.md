```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |   **3,809.9 ns** |   **311.85 ns** |  **17.09 ns** |  **1.00** |    **0.01** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |   3,820.1 ns |    74.25 ns |   4.07 ns |  1.00 |    0.00 |    3 | 0.1450 |      - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          |  21,960.7 ns |   656.30 ns |  35.97 ns |  5.76 |    0.02 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |   2,450.7 ns |   102.21 ns |   5.60 ns |  0.64 |    0.00 |    2 | 0.0916 |      - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     127.3 ns |     5.72 ns |   0.31 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
|                                           |            |              |             |           |       |         |      |        |        |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **16,843.1 ns** |   **366.95 ns** |  **20.11 ns** | **1.000** |    **0.00** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |   3,801.5 ns |   173.21 ns |   9.49 ns | 0.226 |    0.00 |    3 | 0.1526 |      - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 162,731.5 ns | 8,951.98 ns | 490.69 ns | 9.662 |    0.03 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |   2,475.5 ns |   366.82 ns |  20.11 ns | 0.147 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     162.8 ns |     0.57 ns |   0.03 ns | 0.010 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
