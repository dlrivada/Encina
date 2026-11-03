```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.904 μs** | **10.463 μs** | **0.5735 μs** |  **1.03** |    **0.24** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.547 μs |  7.779 μs | 0.4264 μs |  0.55 |    0.16 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.390 μs |  4.399 μs | 0.2411 μs |  0.84 |    0.16 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.872 μs | 14.300 μs | 0.7839 μs |  1.37 |    0.33 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 3.873 μs | 11.129 μs | 0.6100 μs |  1.37 |    0.29 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.737 μs |  8.182 μs | 0.4485 μs |  0.97 |    0.21 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.874 μs |  3.170 μs | 0.1738 μs |  1.01 |    0.18 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.303 μs |  8.607 μs | 0.4718 μs |  1.52 |    0.29 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.431 μs** |  **4.834 μs** | **0.2650 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.724 μs |  1.320 μs | 0.0723 μs |  0.50 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.877 μs | 12.316 μs | 0.6751 μs |  0.84 |    0.18 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.640 μs | 21.617 μs | 1.1849 μs |  1.36 |    0.31 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 6.893 μs | 15.932 μs | 0.8733 μs |  2.02 |    0.26 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.589 μs | 10.014 μs | 0.5489 μs |  1.34 |    0.16 |    4 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.454 μs | 19.323 μs | 1.0592 μs |  1.30 |    0.28 |    4 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.093 μs | 18.876 μs | 1.0347 μs |  1.78 |    0.29 |    5 |     152 B |        2.71 |
