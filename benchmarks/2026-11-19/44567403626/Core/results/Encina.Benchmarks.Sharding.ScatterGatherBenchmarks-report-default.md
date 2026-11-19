
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **3,090.4 ns** | **1,322.75 ns** |  **72.50 ns** |  **1.00** |    **0.03** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   3,142.5 ns |   234.19 ns |  12.84 ns |  1.02 |    0.02 |    3 | 0.0420 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  20,722.9 ns |   329.05 ns |  18.04 ns |  6.71 |    0.13 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,900.8 ns |    37.60 ns |   2.06 ns |  0.62 |    0.01 |    2 | 0.0267 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     142.4 ns |    22.05 ns |   1.21 ns |  0.05 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
                                           |            |              |             |           |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **14,729.8 ns** |   **274.49 ns** |  **15.05 ns** |  **1.00** |    **0.00** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   3,128.8 ns |   100.72 ns |   5.52 ns |  0.21 |    0.00 |    3 | 0.0458 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 160,315.0 ns | 7,297.71 ns | 400.01 ns | 10.88 |    0.03 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,922.7 ns |   101.12 ns |   5.54 ns |  0.13 |    0.00 |    2 | 0.0267 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     170.0 ns |     2.69 ns |   0.15 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
