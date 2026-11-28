```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **5.032 μs** | **20.238 μs** | **1.1093 μs** |  **1.03** |    **0.29** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.799 μs |  9.704 μs | 0.5319 μs |  0.58 |    0.15 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 4.481 μs | 23.286 μs | 1.2764 μs |  0.92 |    0.30 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.019 μs | 32.799 μs | 1.7978 μs |  0.83 |    0.37 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 8.314 μs | 16.782 μs | 0.9199 μs |  1.71 |    0.38 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 6.177 μs | 13.638 μs | 0.7476 μs |  1.27 |    0.29 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.837 μs | 29.974 μs | 1.6430 μs |  0.79 |    0.34 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.487 μs | 26.741 μs | 1.4658 μs |  1.33 |    0.38 |    2 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **4.918 μs** | **10.982 μs** | **0.6019 μs** |  **1.01** |    **0.16** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 3.973 μs | 16.055 μs | 0.8800 μs |  0.82 |    0.18 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.297 μs | 11.725 μs | 0.6427 μs |  0.68 |    0.14 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 6.618 μs | 27.951 μs | 1.5321 μs |  1.36 |    0.31 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 8.665 μs | 19.397 μs | 1.0632 μs |  1.78 |    0.27 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.923 μs | 16.777 μs | 0.9196 μs |  1.01 |    0.20 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 5.894 μs | 12.323 μs | 0.6754 μs |  1.21 |    0.18 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 8.216 μs |  1.932 μs | 0.1059 μs |  1.69 |    0.19 |    4 |     152 B |        2.71 |
