
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,882.6 ns** |    **44.56 ns** |   **2.44 ns** |  **1.00** |    **0.00** |    **3** | **0.1450** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,880.4 ns |   115.50 ns |   6.33 ns |  1.00 |    0.00 |    3 | 0.1450 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  21,704.1 ns |   548.51 ns |  30.07 ns |  5.59 |    0.01 |    4 | 1.1292 | 0.1526 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   2,443.3 ns |   118.66 ns |   6.50 ns |  0.63 |    0.00 |    2 | 0.0916 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     131.6 ns |     4.34 ns |   0.24 ns |  0.03 |    0.00 |    1 | 0.0041 |      - |     104 B |        0.03 |
                                           |            |              |             |           |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **16,816.1 ns** | **1,005.35 ns** |  **55.11 ns** | **1.000** |    **0.00** |    **4** | **0.7629** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,850.8 ns |   124.93 ns |   6.85 ns | 0.229 |    0.00 |    3 | 0.1526 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 165,314.0 ns | 5,871.67 ns | 321.85 ns | 9.831 |    0.03 |    5 | 9.5215 | 6.3477 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   2,534.4 ns |    88.52 ns |   4.85 ns | 0.151 |    0.00 |    2 | 0.0992 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     164.3 ns |    13.73 ns |   0.75 ns | 0.010 |    0.00 |    1 | 0.0110 |      - |     280 B |        0.01 |
