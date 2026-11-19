```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.937 μs** |  **6.1364 μs** | **0.3364 μs** |  **1.01** |    **0.14** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.893 μs |  0.9122 μs | 0.0500 μs |  0.65 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.180 μs |  5.5289 μs | 0.3031 μs |  0.75 |    0.11 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.185 μs |  2.5632 μs | 0.1405 μs |  1.09 |    0.11 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.207 μs |  6.6669 μs | 0.3654 μs |  1.44 |    0.17 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.674 μs |  4.0753 μs | 0.2234 μs |  0.92 |    0.11 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.818 μs |  7.6996 μs | 0.4220 μs |  0.97 |    0.15 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.265 μs |  8.3351 μs | 0.4569 μs |  1.81 |    0.22 |    4 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.663 μs** |  **3.5749 μs** | **0.1960 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.199 μs |  4.0343 μs | 0.2211 μs |  0.60 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.578 μs |  6.6418 μs | 0.3641 μs |  0.71 |    0.09 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.469 μs |  1.3954 μs | 0.0765 μs |  0.95 |    0.05 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 6.883 μs |  5.1971 μs | 0.2849 μs |  1.88 |    0.11 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.165 μs |  8.3923 μs | 0.4600 μs |  0.87 |    0.12 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.305 μs |  6.0144 μs | 0.3297 μs |  0.90 |    0.09 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.024 μs | 11.1916 μs | 0.6135 μs |  1.37 |    0.16 |    3 |     152 B |        2.71 |
