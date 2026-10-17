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
| **&#39;Hash routing&#39;**                   | **3**          | **3.276 μs** | **4.2852 μs** | **0.2349 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.080 μs | 2.8601 μs | 0.1568 μs |  0.64 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.789 μs | 4.2093 μs | 0.2307 μs |  0.85 |    0.08 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.938 μs | 4.8337 μs | 0.2650 μs |  1.21 |    0.10 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.526 μs | 3.8844 μs | 0.2129 μs |  2.00 |    0.13 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.529 μs | 6.6643 μs | 0.3653 μs |  1.08 |    0.12 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 4.168 μs | 3.2839 μs | 0.1800 μs |  1.28 |    0.09 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.418 μs | 2.6455 μs | 0.1450 μs |  1.66 |    0.11 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.446 μs** | **3.5517 μs** | **0.1947 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.770 μs | 2.3392 μs | 0.1282 μs |  0.81 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.397 μs | 2.6387 μs | 0.1446 μs |  0.99 |    0.06 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.202 μs | 7.7074 μs | 0.4225 μs |  1.22 |    0.12 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 8.088 μs | 6.9322 μs | 0.3800 μs |  2.35 |    0.15 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.573 μs | 0.9182 μs | 0.0503 μs |  1.04 |    0.05 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.847 μs | 3.3783 μs | 0.1852 μs |  1.12 |    0.07 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.480 μs | 4.6663 μs | 0.2558 μs |  1.59 |    0.10 |    3 |     152 B |        2.71 |
