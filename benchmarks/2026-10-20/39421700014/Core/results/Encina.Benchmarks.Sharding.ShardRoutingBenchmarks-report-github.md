```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          |  **3.668 μs** | **11.727 μs** | **0.6428 μs** |  **1.02** |    **0.21** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |  2.704 μs |  7.986 μs | 0.4378 μs |  0.75 |    0.15 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |  2.719 μs |  4.141 μs | 0.2270 μs |  0.76 |    0.12 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |  5.339 μs |  2.919 μs | 0.1600 μs |  1.48 |    0.21 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          |  6.296 μs |  4.380 μs | 0.2401 μs |  1.75 |    0.25 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |  3.832 μs |  6.227 μs | 0.3413 μs |  1.06 |    0.17 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |  3.510 μs |  7.386 μs | 0.4048 μs |  0.98 |    0.17 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          |  6.664 μs |  6.314 μs | 0.3461 μs |  1.85 |    0.27 |    3 |     152 B |        2.71 |
|                                  |            |           |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |  **4.104 μs** |  **5.182 μs** | **0.2841 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |  2.274 μs |  1.973 μs | 0.1082 μs |  0.56 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         |  3.940 μs |  2.871 μs | 0.1573 μs |  0.96 |    0.07 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         |  5.457 μs |  3.381 μs | 0.1853 μs |  1.33 |    0.09 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 10.239 μs | 17.661 μs | 0.9680 μs |  2.50 |    0.26 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |  6.182 μs |  9.700 μs | 0.5317 μs |  1.51 |    0.15 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         |  6.001 μs |  7.669 μs | 0.4204 μs |  1.47 |    0.13 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         |  6.238 μs | 11.620 μs | 0.6369 μs |  1.52 |    0.17 |    3 |     152 B |        2.71 |
