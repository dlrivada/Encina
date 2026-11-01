```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | ShardCount | Mean         | Error      | StdDev       | Ratio  | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |-------------:|-----------:|-------------:|-------:|--------:|-----:|--------:|--------:|----------:|------------:|
| **&#39;Scatter-gather all shards (sync result)&#39;** | **3**          |  **1,899.00 ns** |  **19.410 ns** |    **27.837 ns** |   **1.00** |    **0.02** |    **3** |  **0.2174** |       **-** |    **3648 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 3          |  1,899.92 ns |  24.538 ns |    35.191 ns |   1.00 |    0.02 |    3 |  0.2213 |       - |    3704 B |        1.02 |
| &#39;Scatter-gather with large results&#39;       | 3          | 11,529.91 ns | 319.223 ns |   467.913 ns |   6.07 |    0.26 |    4 |  1.7090 |  0.2289 |   28704 B |        7.87 |
| &#39;Scatter-gather single shard&#39;             | 3          |  1,186.11 ns |  15.933 ns |    22.850 ns |   0.62 |    0.01 |    2 |  0.1373 |       - |    2328 B |        0.64 |
| &#39;Topology lookup all shards&#39;              | 3          |     69.96 ns |   0.865 ns |     1.184 ns |   0.04 |    0.00 |    1 |  0.0062 |       - |     104 B |        0.03 |
|                                           |            |              |            |              |        |         |      |         |         |           |             |
| **&#39;Scatter-gather all shards (sync result)&#39;** | **25**         |  **7,931.34 ns** |  **88.519 ns** |   **129.750 ns** |  **1.000** |    **0.02** |    **4** |  **1.1597** |  **0.0305** |   **19464 B** |        **1.00** |
| &#39;Scatter-gather subset (3 shards)&#39;        | 25         |  1,856.45 ns |  17.360 ns |    25.446 ns |  0.234 |    0.00 |    3 |  0.2308 |       - |    3880 B |        0.20 |
| &#39;Scatter-gather with large results&#39;       | 25         | 80,444.16 ns | 926.705 ns | 1,329.053 ns | 10.145 |    0.23 |    5 | 14.4043 | 10.3760 |  242952 B |       12.48 |
| &#39;Scatter-gather single shard&#39;             | 25         |  1,123.72 ns |   8.709 ns |    12.491 ns |  0.142 |    0.00 |    2 |  0.1488 |       - |    2504 B |        0.13 |
| &#39;Topology lookup all shards&#39;              | 25         |     78.61 ns |   1.689 ns |     2.476 ns |  0.010 |    0.00 |    1 |  0.0167 |       - |     280 B |        0.01 |
