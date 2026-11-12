```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.729 μs** |  **3.802 μs** | **0.2084 μs** |  **1.00** |    **0.10** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.696 μs |  2.608 μs | 0.1429 μs |  0.62 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.678 μs | 15.620 μs | 0.8562 μs |  0.99 |    0.28 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.207 μs |  8.599 μs | 0.4714 μs |  1.18 |    0.17 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 3.826 μs |  7.912 μs | 0.4337 μs |  1.41 |    0.17 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.641 μs |  4.738 μs | 0.2597 μs |  0.97 |    0.11 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.774 μs |  3.655 μs | 0.2003 μs |  1.02 |    0.10 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.701 μs |  9.634 μs | 0.5281 μs |  1.73 |    0.21 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.362 μs** |  **3.021 μs** | **0.1656 μs** |  **1.00** |    **0.06** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.890 μs |  5.883 μs | 0.3225 μs |  0.56 |    0.09 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.279 μs |  4.268 μs | 0.2340 μs |  0.68 |    0.07 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.475 μs |  5.850 μs | 0.3207 μs |  1.04 |    0.09 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.779 μs |  2.526 μs | 0.1385 μs |  1.72 |    0.08 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 2.817 μs |  5.766 μs | 0.3160 μs |  0.84 |    0.09 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.319 μs |  6.359 μs | 0.3486 μs |  0.99 |    0.10 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.180 μs |  8.034 μs | 0.4404 μs |  1.54 |    0.13 |    4 |     152 B |        2.71 |
