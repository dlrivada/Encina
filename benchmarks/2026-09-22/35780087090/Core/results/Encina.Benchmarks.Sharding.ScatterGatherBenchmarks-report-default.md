
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.50GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,920.0 ns** |    **92.19 ns** |  **5.05 ns** |  **1.00** |    **0.00** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,829.6 ns |   130.85 ns |  7.17 ns |  0.98 |    0.00 |    3 | 0.1450 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  20,839.7 ns | 1,343.46 ns | 73.64 ns |  5.32 |    0.02 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,482.5 ns |    11.48 ns |  0.63 ns |  0.63 |    0.00 |    2 | 0.0916 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     127.7 ns |    11.63 ns |  0.64 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
                                           |            |              |             |          |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **16,769.9 ns** |   **697.41 ns** | **38.23 ns** | **1.000** |    **0.00** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,962.3 ns |   125.87 ns |  6.90 ns | 0.236 |    0.00 |    3 | 0.1526 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 159,484.8 ns | 1,530.98 ns | 83.92 ns | 9.510 |    0.02 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,647.5 ns |   154.18 ns |  8.45 ns | 0.158 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     154.2 ns |     6.98 ns |  0.38 ns | 0.009 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
