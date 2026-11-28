```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.607 μs** |  **4.311 μs** | **0.2363 μs** |  **1.01** |    **0.11** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.516 μs |  2.839 μs | 0.1556 μs |  0.58 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.066 μs |  4.116 μs | 0.2256 μs |  0.80 |    0.10 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.084 μs |  5.393 μs | 0.2956 μs |  1.19 |    0.13 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 3.850 μs | 11.756 μs | 0.6444 μs |  1.48 |    0.24 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.653 μs |  2.295 μs | 0.1258 μs |  1.02 |    0.09 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.641 μs |  6.236 μs | 0.3418 μs |  1.02 |    0.14 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.797 μs |  4.606 μs | 0.2525 μs |  1.85 |    0.16 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.011 μs** |  **6.084 μs** | **0.3335 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.177 μs |  2.419 μs | 0.1326 μs |  0.73 |    0.08 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.554 μs |  5.709 μs | 0.3129 μs |  0.86 |    0.12 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.184 μs |  3.344 μs | 0.1833 μs |  1.07 |    0.11 |    1 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.519 μs |  9.078 μs | 0.4976 μs |  1.85 |    0.23 |    2 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 2.878 μs |  4.390 μs | 0.2406 μs |  0.96 |    0.11 |    1 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.522 μs |  3.154 μs | 0.1729 μs |  1.18 |    0.12 |    1 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.188 μs |  3.003 μs | 0.1646 μs |  1.74 |    0.17 |    2 |     152 B |        2.71 |
