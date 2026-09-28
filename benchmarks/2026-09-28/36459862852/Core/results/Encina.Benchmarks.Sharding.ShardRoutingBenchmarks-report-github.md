```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.664 μs** |  **2.4826 μs** | **0.1361 μs** |  **1.00** |    **0.05** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.641 μs |  2.4875 μs | 0.1363 μs |  0.72 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.502 μs |  0.9362 μs | 0.0513 μs |  0.68 |    0.03 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.910 μs |  3.1539 μs | 0.1729 μs |  1.07 |    0.05 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.075 μs |  5.5853 μs | 0.3061 μs |  1.66 |    0.09 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.898 μs |  2.8439 μs | 0.1559 μs |  1.06 |    0.05 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.212 μs |  4.6398 μs | 0.2543 μs |  0.88 |    0.07 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.313 μs |  4.8451 μs | 0.2656 μs |  1.45 |    0.08 |    3 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.488 μs** |  **3.2302 μs** | **0.1771 μs** |  **1.00** |    **0.06** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.985 μs |  1.2147 μs | 0.0666 μs |  0.57 |    0.03 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.535 μs |  1.4956 μs | 0.0820 μs |  0.73 |    0.04 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.006 μs | 12.0408 μs | 0.6600 μs |  1.15 |    0.17 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.959 μs | 15.1670 μs | 0.8314 μs |  2.29 |    0.23 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.171 μs |  2.6286 μs | 0.1441 μs |  0.91 |    0.05 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.714 μs |  3.0204 μs | 0.1656 μs |  1.35 |    0.07 |    4 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.502 μs |  5.6736 μs | 0.3110 μs |  1.58 |    0.10 |    4 |     152 B |        2.71 |
