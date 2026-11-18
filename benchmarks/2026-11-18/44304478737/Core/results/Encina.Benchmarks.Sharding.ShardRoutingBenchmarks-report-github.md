```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.039 μs** |   **3.655 μs** |  **0.2003 μs** | **2.966 μs** |  **1.00** |    **0.08** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.993 μs |   1.430 μs |  0.0784 μs | 2.034 μs |  0.66 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 9.242 μs | 185.698 μs | 10.1788 μs | 3.542 μs |  3.05 |    2.92 |    5 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.928 μs |   4.468 μs |  0.2449 μs | 3.818 μs |  1.30 |    0.10 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.177 μs |   5.222 μs |  0.2862 μs | 6.201 μs |  2.04 |    0.14 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.183 μs |   2.655 μs |  0.1455 μs | 3.187 μs |  1.05 |    0.07 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.212 μs |   3.418 μs |  0.1874 μs | 3.135 μs |  1.06 |    0.08 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.515 μs |   7.982 μs |  0.4375 μs | 5.325 μs |  1.82 |    0.16 |    4 |     152 B |        2.71 |
|                                  |            |          |            |            |          |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.440 μs** |   **3.276 μs** |  **0.1796 μs** | **3.347 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.457 μs |   3.664 μs |  0.2008 μs | 2.470 μs |  0.72 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.565 μs |   1.740 μs |  0.0954 μs | 2.515 μs |  0.75 |    0.04 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.924 μs |   2.454 μs |  0.1345 μs | 3.867 μs |  1.14 |    0.06 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.608 μs |   7.143 μs |  0.3915 μs | 7.575 μs |  2.22 |    0.14 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.878 μs |   5.448 μs |  0.2986 μs | 3.928 μs |  1.13 |    0.09 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.697 μs |   2.639 μs |  0.1446 μs | 3.657 μs |  1.08 |    0.06 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.042 μs |   7.524 μs |  0.4124 μs | 5.871 μs |  1.76 |    0.13 |    3 |     152 B |        2.71 |
