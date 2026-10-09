```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.660 μs** | **6.4460 μs** | **0.3533 μs** |  **1.01** |    **0.12** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.317 μs | 8.0965 μs | 0.4438 μs |  0.64 |    0.12 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.641 μs | 1.5729 μs | 0.0862 μs |  0.73 |    0.06 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.069 μs | 2.7505 μs | 0.1508 μs |  1.12 |    0.10 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.094 μs | 6.6313 μs | 0.3635 μs |  1.67 |    0.16 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 4.074 μs | 4.6384 μs | 0.2542 μs |  1.12 |    0.11 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 4.317 μs | 3.5859 μs | 0.1966 μs |  1.19 |    0.11 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.557 μs | 3.5297 μs | 0.1935 μs |  1.53 |    0.13 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.545 μs** | **4.8275 μs** | **0.2646 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.187 μs | 0.8480 μs | 0.0465 μs |  0.62 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.280 μs | 0.9182 μs | 0.0503 μs |  0.93 |    0.06 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.131 μs | 4.9673 μs | 0.2723 μs |  1.17 |    0.10 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 9.283 μs | 7.4955 μs | 0.4109 μs |  2.63 |    0.19 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.408 μs | 0.6407 μs | 0.0351 μs |  0.96 |    0.06 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.272 μs | 4.1083 μs | 0.2252 μs |  1.21 |    0.09 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.957 μs | 4.6461 μs | 0.2547 μs |  1.69 |    0.12 |    3 |     152 B |        2.71 |
