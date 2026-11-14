```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.569 μs** |   **7.727 μs** | **0.4235 μs** | **2.449 μs** |  **1.02** |    **0.20** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.476 μs |   2.982 μs | 0.1635 μs | 1.402 μs |  0.58 |    0.10 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 1.940 μs |   7.191 μs | 0.3942 μs | 1.803 μs |  0.77 |    0.17 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.193 μs |   7.335 μs | 0.4021 μs | 3.110 μs |  1.26 |    0.22 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 3.886 μs |   5.309 μs | 0.2910 μs | 3.866 μs |  1.54 |    0.23 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.937 μs |   6.989 μs | 0.3831 μs | 2.774 μs |  1.16 |    0.21 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.716 μs |   3.224 μs | 0.1767 μs | 2.770 μs |  1.08 |    0.16 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.774 μs |   9.448 μs | 0.5179 μs | 4.716 μs |  1.89 |    0.31 |    5 |     152 B |        2.71 |
|                                  |            |          |            |           |          |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.188 μs** |   **9.688 μs** | **0.5310 μs** | **3.195 μs** |  **1.02** |    **0.21** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.984 μs |   2.556 μs | 0.1401 μs | 1.978 μs |  0.63 |    0.10 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.291 μs |   3.673 μs | 0.2013 μs | 2.264 μs |  0.73 |    0.12 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 8.256 μs | 143.465 μs | 7.8638 μs | 3.736 μs |  2.64 |    2.23 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.692 μs |   6.687 μs | 0.3665 μs | 5.619 μs |  1.82 |    0.29 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.065 μs |   5.395 μs | 0.2957 μs | 2.915 μs |  0.98 |    0.17 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.679 μs |  14.346 μs | 0.7864 μs | 3.225 μs |  1.18 |    0.28 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.841 μs |   6.028 μs | 0.3304 μs | 4.697 μs |  1.55 |    0.25 |    3 |     152 B |        2.71 |
