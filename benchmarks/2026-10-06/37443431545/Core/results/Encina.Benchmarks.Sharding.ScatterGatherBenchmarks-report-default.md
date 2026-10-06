
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.72GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,877.3 ns** |   **245.26 ns** |  **13.44 ns** |  **1.00** |    **0.00** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,804.3 ns |   609.16 ns |  33.39 ns |  0.98 |    0.01 |    3 | 0.1450 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  20,885.9 ns |   324.51 ns |  17.79 ns |  5.39 |    0.02 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,477.4 ns |   232.42 ns |  12.74 ns |  0.64 |    0.00 |    2 | 0.0916 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     129.9 ns |    26.00 ns |   1.43 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
                                           |            |              |             |           |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **17,011.7 ns** |   **561.49 ns** |  **30.78 ns** | **1.000** |    **0.00** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,951.1 ns |   434.68 ns |  23.83 ns | 0.232 |    0.00 |    3 | 0.1526 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 159,689.3 ns | 5,994.93 ns | 328.60 ns | 9.387 |    0.02 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,482.2 ns |   557.61 ns |  30.56 ns | 0.146 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     155.2 ns |     7.96 ns |   0.44 ns | 0.009 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
