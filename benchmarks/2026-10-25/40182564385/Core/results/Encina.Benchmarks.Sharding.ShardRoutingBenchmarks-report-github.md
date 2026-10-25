```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.042 μs** | **3.1100 μs** | **0.1705 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.060 μs | 0.5931 μs | 0.0325 μs |  0.68 |    0.03 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.529 μs | 1.2387 μs | 0.0679 μs |  0.83 |    0.04 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.724 μs | 3.3803 μs | 0.1853 μs |  1.23 |    0.08 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 7.275 μs | 8.4426 μs | 0.4628 μs |  2.40 |    0.17 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.146 μs | 4.2614 μs | 0.2336 μs |  1.04 |    0.08 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.282 μs | 3.8050 μs | 0.2086 μs |  1.08 |    0.08 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.376 μs | 4.1522 μs | 0.2276 μs |  1.77 |    0.11 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.480 μs** | **4.0056 μs** | **0.2196 μs** |  **1.00** |    **0.08** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.131 μs | 0.6407 μs | 0.0351 μs |  0.61 |    0.03 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.646 μs | 1.0256 μs | 0.0562 μs |  0.76 |    0.04 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.840 μs | 3.1403 μs | 0.1721 μs |  1.11 |    0.07 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.678 μs | 7.4073 μs | 0.4060 μs |  2.21 |    0.16 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.288 μs | 2.6657 μs | 0.1461 μs |  0.95 |    0.06 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.854 μs | 4.3041 μs | 0.2359 μs |  1.11 |    0.08 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.558 μs | 2.5951 μs | 0.1422 μs |  1.60 |    0.09 |    4 |     152 B |        2.71 |
