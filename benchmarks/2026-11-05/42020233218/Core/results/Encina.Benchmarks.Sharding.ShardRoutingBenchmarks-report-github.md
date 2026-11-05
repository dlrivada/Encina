```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.663 μs** |  **0.6417 μs** | **0.0352 μs** |  **1.00** |    **0.01** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.040 μs |  0.5903 μs | 0.0324 μs |  0.56 |    0.01 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.968 μs | 13.7286 μs | 0.7525 μs |  0.81 |    0.18 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.793 μs |  1.9163 μs | 0.1050 μs |  1.04 |    0.03 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.266 μs |  6.1322 μs | 0.3361 μs |  1.71 |    0.08 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.316 μs |  4.1610 μs | 0.2281 μs |  0.91 |    0.05 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 4.050 μs |  4.0067 μs | 0.2196 μs |  1.11 |    0.05 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.780 μs |  2.7547 μs | 0.1510 μs |  1.58 |    0.04 |    4 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.280 μs** |  **2.6551 μs** | **0.1455 μs** |  **1.00** |    **0.05** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.588 μs |  4.2172 μs | 0.2312 μs |  0.79 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.632 μs |  1.1147 μs | 0.0611 μs |  0.80 |    0.03 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.840 μs |  2.4986 μs | 0.1370 μs |  1.17 |    0.06 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.805 μs |  4.2079 μs | 0.2307 μs |  2.38 |    0.11 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.105 μs |  2.8090 μs | 0.1540 μs |  1.25 |    0.06 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.680 μs |  2.5169 μs | 0.1380 μs |  1.12 |    0.06 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.684 μs |  2.5628 μs | 0.1405 μs |  1.74 |    0.08 |    3 |     152 B |        2.71 |
