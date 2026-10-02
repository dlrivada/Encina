
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,312.4 ns** |    **395.86 ns** |    **21.70 ns** |  **1.00** |    **0.01** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,277.0 ns |    304.91 ns |    16.71 ns |  0.98 |    0.01 |    3 | 0.0420 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  15,691.5 ns |  9,689.66 ns |   531.12 ns |  6.79 |    0.21 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,352.9 ns |     95.39 ns |     5.23 ns |  0.59 |    0.01 |    2 | 0.0267 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     112.9 ns |      6.74 ns |     0.37 ns |  0.05 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
                                           |            |              |              |             |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **10,715.7 ns** |     **43.06 ns** |     **2.36 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,305.0 ns |    166.28 ns |     9.11 ns |  0.22 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 123,907.7 ns | 76,056.92 ns | 4,168.94 ns | 11.56 |    0.34 |    5 | 2.8076 | 1.8311 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,417.5 ns |     43.75 ns |     2.40 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     135.5 ns |     57.61 ns |     3.16 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
