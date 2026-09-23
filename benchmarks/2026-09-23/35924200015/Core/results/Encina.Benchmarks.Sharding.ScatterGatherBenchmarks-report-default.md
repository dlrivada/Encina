
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                    | ShardCount | Mean         | Error        | StdDev       | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
------------------------------------------ |----------- |-------------:|-------------:|-------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          |  **1,905.76 ns** |  **1,954.94 ns** |   **107.157 ns** |   **1.00** |    **0.07** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          |  1,790.04 ns |    424.39 ns |    23.262 ns |   0.94 |    0.05 |    3 |  0.2213 |       - |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 10,444.55 ns |  2,052.33 ns |   112.495 ns |   5.49 |    0.27 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          |  1,218.78 ns |    159.82 ns |     8.760 ns |   0.64 |    0.03 |    2 |  0.1373 |       - |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |     72.69 ns |     61.93 ns |     3.395 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
                                           |            |              |              |              |        |         |      |         |         |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         |  **8,728.84 ns** |  **9,063.45 ns** |   **496.798 ns** |  **1.002** |    **0.07** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         |  1,966.75 ns |    268.32 ns |    14.707 ns |  0.226 |    0.01 |    3 |  0.2289 |       - |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 89,936.73 ns | 82,607.53 ns | 4,527.997 ns | 10.325 |    0.67 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         |  1,283.27 ns |  1,392.48 ns |    76.327 ns |  0.147 |    0.01 |    2 |  0.1488 |       - |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |     83.37 ns |     13.09 ns |     0.718 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
