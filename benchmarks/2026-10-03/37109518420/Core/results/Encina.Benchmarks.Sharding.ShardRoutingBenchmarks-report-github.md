```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          |  **3.140 μs** |  **3.405 μs** | **0.1866 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |  2.038 μs |  2.032 μs | 0.1114 μs |  0.65 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |  3.046 μs |  4.464 μs | 0.2447 μs |  0.97 |    0.08 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |  3.833 μs |  5.350 μs | 0.2933 μs |  1.22 |    0.10 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          |  5.985 μs |  4.235 μs | 0.2321 μs |  1.91 |    0.12 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |  3.333 μs |  9.030 μs | 0.4950 μs |  1.06 |    0.15 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |  4.105 μs |  2.075 μs | 0.1137 μs |  1.31 |    0.07 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          |  5.734 μs |  6.225 μs | 0.3412 μs |  1.83 |    0.13 |    3 |     152 B |        2.71 |
|                                  |            |           |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |  **4.274 μs** | **13.893 μs** | **0.7615 μs** |  **1.02** |    **0.23** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |  2.711 μs |  3.665 μs | 0.2009 μs |  0.65 |    0.12 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         |  2.561 μs |  3.222 μs | 0.1766 μs |  0.61 |    0.11 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         |  4.906 μs |  2.920 μs | 0.1600 μs |  1.17 |    0.20 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         |  7.781 μs |  5.246 μs | 0.2875 μs |  1.86 |    0.32 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |  3.459 μs |  5.812 μs | 0.3186 μs |  0.83 |    0.15 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         |  5.399 μs |  3.356 μs | 0.1839 μs |  1.29 |    0.22 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 12.451 μs | 50.596 μs | 2.7734 μs |  2.98 |    0.77 |    5 |     152 B |        2.71 |
