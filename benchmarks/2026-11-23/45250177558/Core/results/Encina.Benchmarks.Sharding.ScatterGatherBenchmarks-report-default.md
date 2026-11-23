
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,414.5 ns** |   **106.76 ns** |   **5.85 ns** |  **1.00** |    **0.00** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,412.5 ns |   355.23 ns |  19.47 ns |  1.00 |    0.01 |    3 | 0.0420 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  15,795.9 ns |   426.93 ns |  23.40 ns |  6.54 |    0.02 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,475.0 ns |    14.30 ns |   0.78 ns |  0.61 |    0.00 |    2 | 0.0267 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     102.0 ns |     1.87 ns |   0.10 ns |  0.04 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
                                           |            |              |             |           |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **11,615.2 ns** |   **460.21 ns** |  **25.23 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,413.6 ns |   218.79 ns |  11.99 ns |  0.21 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 123,985.9 ns | 6,847.01 ns | 375.31 ns | 10.67 |    0.03 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,460.6 ns |    78.59 ns |   4.31 ns |  0.13 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     139.3 ns |    19.97 ns |   1.09 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
