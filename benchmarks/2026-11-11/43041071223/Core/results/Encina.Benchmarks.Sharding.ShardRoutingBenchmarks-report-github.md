```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.471 μs** |  **7.275 μs** | **0.3988 μs** |  **1.02** |    **0.19** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.623 μs |  2.040 μs | 0.1118 μs |  0.67 |    0.10 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 1.986 μs |  2.658 μs | 0.1457 μs |  0.82 |    0.12 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 2.959 μs |  7.161 μs | 0.3925 μs |  1.22 |    0.21 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.096 μs | 12.228 μs | 0.6703 μs |  1.69 |    0.33 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.647 μs |  4.188 μs | 0.2295 μs |  1.09 |    0.16 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.731 μs |  2.538 μs | 0.1391 μs |  1.12 |    0.15 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.048 μs | 10.824 μs | 0.5933 μs |  2.08 |    0.34 |    5 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **2.948 μs** |  **3.908 μs** | **0.2142 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.149 μs |  1.414 μs | 0.0775 μs |  0.73 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.278 μs |  1.287 μs | 0.0706 μs |  0.78 |    0.05 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.502 μs | 10.705 μs | 0.5868 μs |  1.19 |    0.19 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 6.594 μs |  7.555 μs | 0.4141 μs |  2.24 |    0.18 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.195 μs |  4.444 μs | 0.2436 μs |  1.09 |    0.10 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.422 μs |  2.968 μs | 0.1627 μs |  1.16 |    0.09 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.521 μs |  6.770 μs | 0.3711 μs |  1.54 |    0.14 |    3 |     152 B |        2.71 |
