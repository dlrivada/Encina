```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.411 μs** |  **4.333 μs** | **0.2375 μs** |  **1.01** |    **0.12** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.419 μs |  4.997 μs | 0.2739 μs |  0.59 |    0.11 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 1.865 μs |  4.818 μs | 0.2641 μs |  0.78 |    0.11 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 2.973 μs |  6.024 μs | 0.3302 μs |  1.24 |    0.16 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 3.963 μs | 15.887 μs | 0.8708 μs |  1.65 |    0.34 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.547 μs | 10.250 μs | 0.5618 μs |  1.06 |    0.22 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.646 μs |  3.768 μs | 0.2066 μs |  1.10 |    0.12 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.212 μs | 11.607 μs | 0.6362 μs |  1.76 |    0.27 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.013 μs** |  **7.492 μs** | **0.4107 μs** |  **1.01** |    **0.17** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.794 μs |  3.311 μs | 0.1815 μs |  0.60 |    0.09 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.093 μs | 18.943 μs | 1.0383 μs |  1.04 |    0.33 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.128 μs |  4.350 μs | 0.2384 μs |  1.05 |    0.14 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.603 μs |  6.283 μs | 0.3444 μs |  1.88 |    0.24 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 2.825 μs |  4.866 μs | 0.2667 μs |  0.95 |    0.14 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.255 μs |  6.661 μs | 0.3651 μs |  1.09 |    0.17 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.633 μs |  8.486 μs | 0.4652 μs |  1.56 |    0.23 |    3 |     152 B |        2.71 |
