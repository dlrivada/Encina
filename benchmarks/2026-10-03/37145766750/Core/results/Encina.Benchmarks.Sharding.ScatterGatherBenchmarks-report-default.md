
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.66GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,908.3 ns** |   **349.73 ns** |  **19.17 ns** |  **1.00** |    **0.01** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,848.3 ns |   926.09 ns |  50.76 ns |  0.98 |    0.01 |    3 | 0.1450 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  21,127.8 ns | 1,425.60 ns |  78.14 ns |  5.41 |    0.03 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,445.9 ns |    82.96 ns |   4.55 ns |  0.63 |    0.00 |    2 | 0.0916 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     128.7 ns |     3.60 ns |   0.20 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
                                           |            |              |             |           |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **16,498.8 ns** |   **524.79 ns** |  **28.77 ns** | **1.000** |    **0.00** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,810.2 ns |   297.84 ns |  16.33 ns | 0.231 |    0.00 |    3 | 0.1526 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 157,919.3 ns | 7,534.58 ns | 413.00 ns | 9.572 |    0.03 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,440.1 ns |   481.27 ns |  26.38 ns | 0.148 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     153.2 ns |    15.10 ns |   0.83 ns | 0.009 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
