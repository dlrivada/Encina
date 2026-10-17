
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |  **1,680.98 ns** |   **108.637 ns** |   **5.955 ns** |  **1.00** |    **0.00** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |  1,693.77 ns |   138.240 ns |   7.577 ns |  1.01 |    0.00 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 10,202.73 ns |   559.560 ns |  30.671 ns |  6.07 |    0.02 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |  1,094.63 ns |    52.266 ns |   2.865 ns |  0.65 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     64.91 ns |     5.536 ns |   0.303 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |            |       |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **7,515.52 ns** |   **411.151 ns** |  **22.537 ns** |  **1.00** |    **0.00** |    **4** |  **1.1597** |  **0.0381** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |  1,733.61 ns |   578.743 ns |  31.723 ns |  0.23 |    0.00 |    3 |  0.2308 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 77,334.30 ns | 8,881.147 ns | 486.806 ns | 10.29 |    0.06 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |  1,134.11 ns |   703.232 ns |  38.547 ns |  0.15 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     75.29 ns |     5.648 ns |   0.310 ns |  0.01 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
