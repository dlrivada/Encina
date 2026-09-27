
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |   **2,773.7 ns** |  **1,540.07 ns** |    **84.42 ns** |  **1.00** |    **0.04** |    **3** | **0.0420** |      **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |   2,893.5 ns |    424.22 ns |    23.25 ns |  1.04 |    0.03 |    3 | 0.0420 |      - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          |  18,580.9 ns |  1,833.67 ns |   100.51 ns |  6.70 |    0.18 |    4 | 0.3357 | 0.0305 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |   1,652.0 ns |    632.59 ns |    34.67 ns |  0.60 |    0.02 |    2 | 0.0267 |      - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     142.8 ns |    121.19 ns |     6.64 ns |  0.05 |    0.00 |    1 | 0.0012 |      - |     104 B |        0.03 |
                                           |            |              |              |             |       |         |      |        |        |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **12,557.9 ns** |  **7,033.57 ns** |   **385.53 ns** |  **1.00** |    **0.04** |    **4** | **0.2289** |      **-** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |   2,695.9 ns |    425.18 ns |    23.31 ns |  0.21 |    0.01 |    3 | 0.0458 |      - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 145,014.3 ns | 71,608.14 ns | 3,925.08 ns | 11.55 |    0.41 |    5 | 2.6855 | 1.7090 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |   1,702.0 ns |    647.47 ns |    35.49 ns |  0.14 |    0.00 |    2 | 0.0286 |      - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     138.1 ns |      6.54 ns |     0.36 ns |  0.01 |    0.00 |    1 | 0.0033 |      - |     280 B |        0.01 |
