
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                                    | ShardCount | Mean         | Error        | StdDev       | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|-------------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |  **1,840.08 ns** |    **22.980 ns** |    **33.684 ns** |  **1.00** |    **0.03** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |  1,780.01 ns |    35.288 ns |    51.725 ns |  0.97 |    0.03 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 10,488.68 ns |   173.016 ns |   253.605 ns |  5.70 |    0.17 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |  1,106.41 ns |     5.740 ns |     8.047 ns |  0.60 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     66.01 ns |     0.831 ns |     1.165 ns |  0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |              |       |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **7,914.36 ns** |   **132.664 ns** |   **185.976 ns** |  **1.00** |    **0.03** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |  1,887.39 ns |    38.912 ns |    57.037 ns |  0.24 |    0.01 |    3 |  0.2308 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 81,015.91 ns | 1,595.863 ns | 2,288.740 ns | 10.24 |    0.37 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |  1,163.99 ns |    13.703 ns |    19.652 ns |  0.15 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     81.03 ns |     2.264 ns |     3.319 ns |  0.01 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
