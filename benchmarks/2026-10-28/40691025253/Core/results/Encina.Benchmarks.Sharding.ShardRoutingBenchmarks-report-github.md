```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.425 μs** | **10.131 μs** | **0.5553 μs** |  **1.02** |    **0.21** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.016 μs |  2.304 μs | 0.1263 μs |  0.60 |    0.09 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.753 μs |  3.778 μs | 0.2071 μs |  0.82 |    0.13 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.773 μs |  6.856 μs | 0.3758 μs |  1.12 |    0.19 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.744 μs | 13.641 μs | 0.7477 μs |  1.41 |    0.28 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.332 μs |  8.194 μs | 0.4492 μs |  0.99 |    0.19 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.727 μs |  6.007 μs | 0.3292 μs |  1.11 |    0.18 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.822 μs | 11.688 μs | 0.6407 μs |  1.73 |    0.30 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.829 μs** |  **1.225 μs** | **0.0672 μs** |  **1.00** |    **0.02** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.294 μs |  1.269 μs | 0.0696 μs |  0.60 |    0.02 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.036 μs |  8.885 μs | 0.4870 μs |  0.79 |    0.11 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 6.439 μs |  7.416 μs | 0.4065 μs |  1.68 |    0.10 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.260 μs | 10.413 μs | 0.5708 μs |  1.90 |    0.13 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.488 μs |  4.116 μs | 0.2256 μs |  0.91 |    0.05 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.176 μs |  4.083 μs | 0.2238 μs |  1.09 |    0.05 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.784 μs |  5.547 μs | 0.3041 μs |  1.51 |    0.07 |    3 |     152 B |        2.71 |
