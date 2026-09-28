
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|--------------:|-------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |  **1,728.79 ns** |    **197.163 ns** |    **10.807 ns** |  **1.00** |    **0.01** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |  1,764.75 ns |    122.766 ns |     6.729 ns |  1.02 |    0.01 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 10,420.21 ns |  7,772.502 ns |   426.037 ns |  6.03 |    0.22 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |  1,105.01 ns |     75.162 ns |     4.120 ns |  0.64 |    0.00 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     65.87 ns |     13.295 ns |     0.729 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |               |              |       |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **7,706.00 ns** |  **1,082.347 ns** |    **59.327 ns** | **1.000** |    **0.01** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |  1,722.13 ns |     62.743 ns |     3.439 ns | 0.223 |    0.00 |    3 |  0.2308 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 77,031.86 ns | 29,273.056 ns | 1,604.555 ns | 9.997 |    0.19 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |  1,118.17 ns |    932.460 ns |    51.111 ns | 0.145 |    0.01 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     76.10 ns |      9.207 ns |     0.505 ns | 0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
