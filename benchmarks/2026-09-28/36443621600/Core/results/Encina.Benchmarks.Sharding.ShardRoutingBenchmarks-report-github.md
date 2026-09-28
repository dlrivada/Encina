```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.541 μs** |  **7.511 μs** | **0.4117 μs** |  **1.01** |    **0.15** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.445 μs | 12.978 μs | 0.7114 μs |  0.70 |    0.19 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 3.460 μs | 19.268 μs | 1.0561 μs |  0.99 |    0.28 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.524 μs |  2.982 μs | 0.1635 μs |  1.29 |    0.14 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.547 μs | 10.795 μs | 0.5917 μs |  1.87 |    0.24 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.730 μs |  8.785 μs | 0.4815 μs |  1.06 |    0.16 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 4.152 μs | 13.362 μs | 0.7324 μs |  1.18 |    0.22 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.536 μs |  6.219 μs | 0.3409 μs |  1.86 |    0.21 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **5.330 μs** |  **6.671 μs** | **0.3657 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.465 μs |  5.272 μs | 0.2890 μs |  0.46 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.902 μs |  8.521 μs | 0.4671 μs |  0.55 |    0.08 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.988 μs |  2.062 μs | 0.1130 μs |  0.75 |    0.05 |    1 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 9.212 μs |  7.776 μs | 0.4262 μs |  1.73 |    0.13 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.269 μs |  2.949 μs | 0.1616 μs |  0.62 |    0.05 |    1 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.837 μs |  2.565 μs | 0.1406 μs |  0.72 |    0.05 |    1 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.643 μs |  6.453 μs | 0.3537 μs |  1.06 |    0.09 |    2 |     152 B |        2.71 |
