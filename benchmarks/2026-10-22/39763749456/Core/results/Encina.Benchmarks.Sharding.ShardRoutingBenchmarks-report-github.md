```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.604 μs** |  **7.2170 μs** | **0.3956 μs** |  **1.02** |    **0.19** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.412 μs |  4.4494 μs | 0.2439 μs |  0.55 |    0.11 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.085 μs |  4.8005 μs | 0.2631 μs |  0.81 |    0.14 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 2.797 μs |  8.6553 μs | 0.4744 μs |  1.09 |    0.22 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 3.952 μs |  7.3188 μs | 0.4012 μs |  1.54 |    0.25 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.610 μs |  4.0296 μs | 0.2209 μs |  1.02 |    0.15 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.895 μs |  0.9654 μs | 0.0529 μs |  1.13 |    0.15 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.364 μs |  7.8530 μs | 0.4304 μs |  1.70 |    0.27 |    3 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **2.911 μs** |  **6.8700 μs** | **0.3766 μs** |  **1.01** |    **0.16** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.753 μs |  2.1072 μs | 0.1155 μs |  0.61 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.022 μs |  4.7539 μs | 0.2606 μs |  0.70 |    0.11 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.392 μs |  4.7303 μs | 0.2593 μs |  1.18 |    0.15 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.858 μs | 52.0177 μs | 2.8513 μs |  2.73 |    0.91 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.161 μs |  1.3042 μs | 0.0715 μs |  1.10 |    0.12 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 2.999 μs |  4.4126 μs | 0.2419 μs |  1.04 |    0.13 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.460 μs | 11.7627 μs | 0.6448 μs |  1.55 |    0.25 |    3 |     152 B |        2.71 |
