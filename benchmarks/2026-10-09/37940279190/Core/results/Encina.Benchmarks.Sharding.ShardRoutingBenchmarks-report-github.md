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
| **&#39;Hash routing&#39;**                   | **3**          | **3.172 μs** |  **0.8227 μs** | **0.0451 μs** |  **1.00** |    **0.02** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.036 μs |  0.7595 μs | 0.0416 μs |  0.64 |    0.01 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.626 μs |  0.4896 μs | 0.0268 μs |  0.83 |    0.01 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.457 μs |  5.9394 μs | 0.3256 μs |  1.41 |    0.09 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.162 μs |  4.1722 μs | 0.2287 μs |  1.94 |    0.07 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.176 μs |  2.3822 μs | 0.1306 μs |  1.00 |    0.04 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.367 μs |  2.5865 μs | 0.1418 μs |  1.06 |    0.04 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.520 μs |  4.4903 μs | 0.2461 μs |  1.74 |    0.07 |    5 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.442 μs** |  **1.3918 μs** | **0.0763 μs** |  **1.00** |    **0.03** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.773 μs |  2.7732 μs | 0.1520 μs |  0.81 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.756 μs |  2.5404 μs | 0.1392 μs |  0.80 |    0.04 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.358 μs | 10.6602 μs | 0.5843 μs |  1.27 |    0.15 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 9.378 μs |  5.3855 μs | 0.2952 μs |  2.73 |    0.09 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.434 μs |  1.0533 μs | 0.0577 μs |  1.00 |    0.02 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.445 μs |  9.0912 μs | 0.4983 μs |  1.29 |    0.13 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.649 μs |  7.4679 μs | 0.4093 μs |  1.93 |    0.11 |    3 |     152 B |        2.71 |
