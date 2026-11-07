```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.391 μs** |  **6.2550 μs** | **0.3429 μs** |  **1.01** |    **0.12** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.956 μs |  8.2191 μs | 0.4505 μs |  0.58 |    0.13 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.888 μs |  5.7195 μs | 0.3135 μs |  0.86 |    0.11 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.039 μs |  9.7778 μs | 0.5360 μs |  1.20 |    0.17 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 5.088 μs |  7.4509 μs | 0.4084 μs |  1.51 |    0.17 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.348 μs | 10.2742 μs | 0.5632 μs |  0.99 |    0.17 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.374 μs |  7.2943 μs | 0.3998 μs |  1.00 |    0.13 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.646 μs | 10.5822 μs | 0.5800 μs |  1.68 |    0.21 |    3 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **4.288 μs** |  **7.7598 μs** | **0.4253 μs** |  **1.01** |    **0.13** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.333 μs |  0.1824 μs | 0.0100 μs |  0.55 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.247 μs |  4.7084 μs | 0.2581 μs |  0.76 |    0.09 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.657 μs | 11.5248 μs | 0.6317 μs |  1.09 |    0.16 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.115 μs | 10.8089 μs | 0.5925 μs |  1.67 |    0.19 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.153 μs | 11.8924 μs | 0.6519 μs |  0.98 |    0.16 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.717 μs |  4.8337 μs | 0.2650 μs |  1.11 |    0.11 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.109 μs |  8.7420 μs | 0.4792 μs |  1.43 |    0.16 |    4 |     152 B |        2.71 |
