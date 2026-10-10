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
| **&#39;Hash routing&#39;**                   | **3**          | **3.914 μs** | **11.0132 μs** | **0.6037 μs** |  **1.01** |    **0.18** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.993 μs |  2.5968 μs | 0.1423 μs |  0.52 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.415 μs |  2.3717 μs | 0.1300 μs |  0.63 |    0.08 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.948 μs |  4.4639 μs | 0.2447 μs |  1.02 |    0.14 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.014 μs |  4.1142 μs | 0.2255 μs |  1.56 |    0.20 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.162 μs |  1.9062 μs | 0.1045 μs |  0.82 |    0.10 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.350 μs |  1.1959 μs | 0.0656 μs |  0.87 |    0.11 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.521 μs | 12.3931 μs | 0.6793 μs |  1.43 |    0.23 |    4 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.493 μs** |  **3.2528 μs** | **0.1783 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 3.537 μs | 26.7363 μs | 1.4655 μs |  1.01 |    0.37 |    3 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.471 μs |  1.8274 μs | 0.1002 μs |  0.71 |    0.04 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.646 μs |  4.4465 μs | 0.2437 μs |  1.33 |    0.08 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.782 μs |  5.9422 μs | 0.3257 μs |  2.23 |    0.13 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.093 μs |  0.3740 μs | 0.0205 μs |  1.17 |    0.05 |    4 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.694 μs |  4.1959 μs | 0.2300 μs |  1.06 |    0.07 |    4 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.464 μs |  4.0668 μs | 0.2229 μs |  1.57 |    0.09 |    4 |     152 B |        2.71 |
