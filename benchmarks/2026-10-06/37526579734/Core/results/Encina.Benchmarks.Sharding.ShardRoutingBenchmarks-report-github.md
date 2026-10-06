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
| **&#39;Hash routing&#39;**                   | **3**          | **3.076 μs** |  **6.372 μs** | **0.3492 μs** |  **1.01** |    **0.14** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.014 μs |  2.597 μs | 0.1423 μs |  0.66 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.455 μs |  3.474 μs | 0.1904 μs |  0.80 |    0.09 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.938 μs |  9.064 μs | 0.4968 μs |  1.29 |    0.19 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 5.905 μs |  8.802 μs | 0.4825 μs |  1.94 |    0.23 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.053 μs |  3.066 μs | 0.1680 μs |  1.00 |    0.11 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.316 μs |  1.567 μs | 0.0859 μs |  1.09 |    0.11 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.644 μs | 16.605 μs | 0.9102 μs |  1.85 |    0.31 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.380 μs** |  **3.604 μs** | **0.1976 μs** |  **1.00** |    **0.07** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.123 μs |  1.559 μs | 0.0854 μs |  0.63 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.584 μs |  1.435 μs | 0.0787 μs |  0.77 |    0.04 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.705 μs |  1.794 μs | 0.0983 μs |  1.40 |    0.07 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.453 μs |  9.566 μs | 0.5243 μs |  2.21 |    0.17 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.627 μs |  5.941 μs | 0.3256 μs |  1.08 |    0.10 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.455 μs | 16.723 μs | 0.9166 μs |  1.32 |    0.24 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.811 μs |  8.950 μs | 0.4906 μs |  1.72 |    0.15 |    4 |     152 B |        2.71 |
