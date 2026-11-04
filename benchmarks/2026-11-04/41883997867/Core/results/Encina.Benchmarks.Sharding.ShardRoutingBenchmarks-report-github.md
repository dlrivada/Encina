```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.767 μs** |   **6.125 μs** | **0.3358 μs** | **2.865 μs** |  **1.01** |    **0.15** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.751 μs |   4.122 μs | 0.2260 μs | 1.657 μs |  0.64 |    0.10 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.013 μs |   6.773 μs | 0.3712 μs | 1.913 μs |  0.73 |    0.14 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.169 μs |   7.431 μs | 0.4073 μs | 3.035 μs |  1.16 |    0.18 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.349 μs |  16.920 μs | 0.9275 μs | 4.096 μs |  1.59 |    0.34 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.737 μs |   7.871 μs | 0.4314 μs | 2.514 μs |  1.00 |    0.18 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.615 μs |   5.896 μs | 0.3232 μs | 2.739 μs |  0.95 |    0.15 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.339 μs |  10.223 μs | 0.5604 μs | 4.546 μs |  1.58 |    0.25 |    3 |     152 B |        2.71 |
|                                  |            |          |            |           |          |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **2.953 μs** |   **7.931 μs** | **0.4347 μs** | **2.850 μs** |  **1.01** |    **0.18** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.575 μs |   2.312 μs | 0.1267 μs | 1.551 μs |  0.54 |    0.08 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 7.638 μs | 154.905 μs | 8.4909 μs | 3.055 μs |  2.62 |    2.56 |    5 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.403 μs |   3.581 μs | 0.1963 μs | 3.289 μs |  1.17 |    0.15 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 6.559 μs |   4.925 μs | 0.2700 μs | 6.429 μs |  2.25 |    0.29 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.736 μs |   4.243 μs | 0.2326 μs | 3.776 μs |  1.28 |    0.17 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.499 μs |   9.731 μs | 0.5334 μs | 3.286 μs |  1.20 |    0.22 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.885 μs |   9.167 μs | 0.5025 μs | 4.981 μs |  1.68 |    0.25 |    3 |     152 B |        2.71 |
