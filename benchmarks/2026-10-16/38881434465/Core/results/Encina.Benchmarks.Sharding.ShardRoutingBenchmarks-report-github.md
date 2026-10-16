```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.269 μs** |  **3.0484 μs** | **0.1671 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.977 μs |  0.9362 μs | 0.0513 μs |  0.61 |    0.03 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 3.486 μs |  8.5649 μs | 0.4695 μs |  1.07 |    0.13 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.465 μs |  3.3811 μs | 0.1853 μs |  1.37 |    0.08 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 8.043 μs | 26.2760 μs | 1.4403 μs |  2.46 |    0.40 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.123 μs |  4.9954 μs | 0.2738 μs |  0.96 |    0.08 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.242 μs |  4.2197 μs | 0.2313 μs |  0.99 |    0.08 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.132 μs |  2.6974 μs | 0.1479 μs |  1.88 |    0.09 |    4 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.442 μs** |  **2.2374 μs** | **0.1226 μs** |  **1.00** |    **0.04** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.170 μs |  1.7276 μs | 0.0947 μs |  0.63 |    0.03 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.672 μs |  1.2207 μs | 0.0669 μs |  0.78 |    0.03 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.987 μs |  6.6563 μs | 0.3649 μs |  1.16 |    0.10 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.750 μs |  6.6711 μs | 0.3657 μs |  2.25 |    0.11 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.419 μs |  3.8418 μs | 0.2106 μs |  0.99 |    0.06 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.947 μs |  6.2331 μs | 0.3417 μs |  1.15 |    0.09 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.059 μs | 14.3748 μs | 0.7879 μs |  1.76 |    0.21 |    4 |     152 B |        2.71 |
