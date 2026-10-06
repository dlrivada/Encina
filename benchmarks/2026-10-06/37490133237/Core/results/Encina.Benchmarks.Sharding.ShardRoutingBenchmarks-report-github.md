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
| **&#39;Hash routing&#39;**                   | **3**          | **3.268 μs** |  **9.2739 μs** | **0.5083 μs** |  **1.02** |    **0.19** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.079 μs |  3.5291 μs | 0.1934 μs |  0.65 |    0.10 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.515 μs |  0.9480 μs | 0.0520 μs |  0.78 |    0.10 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 5.806 μs |  3.4817 μs | 0.1908 μs |  1.80 |    0.23 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 7.954 μs |  6.5975 μs | 0.3616 μs |  2.47 |    0.32 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.610 μs |  6.6731 μs | 0.3658 μs |  1.12 |    0.17 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.305 μs |  3.5291 μs | 0.1934 μs |  1.03 |    0.14 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.540 μs |  3.3352 μs | 0.1828 μs |  1.72 |    0.22 |    3 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **4.416 μs** | **22.2848 μs** | **1.2215 μs** |  **1.05** |    **0.34** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.705 μs |  8.3993 μs | 0.4604 μs |  0.64 |    0.17 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.504 μs |  0.6268 μs | 0.0344 μs |  0.59 |    0.12 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 5.313 μs |  7.1108 μs | 0.3898 μs |  1.26 |    0.28 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 8.586 μs | 19.6904 μs | 1.0793 μs |  2.04 |    0.48 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.245 μs | 19.0056 μs | 1.0418 μs |  1.01 |    0.30 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.144 μs | 12.2155 μs | 0.6696 μs |  0.98 |    0.25 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.114 μs |  7.9066 μs | 0.4334 μs |  1.45 |    0.32 |    3 |     152 B |        2.71 |
