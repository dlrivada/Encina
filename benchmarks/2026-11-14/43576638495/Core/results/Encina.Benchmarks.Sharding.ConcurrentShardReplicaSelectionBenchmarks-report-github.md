```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | ThreadCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Completed Work Items | Lock Contentions | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |------------ |----------:|----------:|---------:|------:|--------:|---------------------:|-----------------:|-------:|----------:|------------:|
| **&#39;Concurrent RoundRobin&#39;**       | **1**           |  **17.26 μs** |  **0.413 μs** | **0.023 μs** |  **1.00** |    **0.00** |                    **-** |                **-** |      **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 1           |  20.56 μs |  1.388 μs | 0.076 μs |  1.19 |    0.00 |                    - |                - |      - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 1           | 114.25 μs |  4.577 μs | 0.251 μs |  6.62 |    0.01 |                    - |                - |      - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 1           | 110.53 μs |  3.914 μs | 0.215 μs |  6.41 |    0.01 |                    - |                - |      - |   1.47 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 1           |  82.55 μs |  0.366 μs | 0.020 μs |  4.78 |    0.01 |                    - |                - |      - |   1.47 KB |        1.00 |
|                               |             |           |           |          |       |         |                      |                  |        |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **4**           |  **38.38 μs** | **30.589 μs** | **1.677 μs** |  **1.00** |    **0.05** |               **2.9996** |                **-** |      **-** |   **2.05 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 4           |  12.15 μs |  2.130 μs | 0.117 μs |  0.32 |    0.01 |               2.9987 |                - | 0.0153 |   2.05 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 4           |  49.71 μs |  2.680 μs | 0.147 μs |  1.30 |    0.05 |               2.9995 |                - |      - |   2.05 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 4           |  48.32 μs |  0.888 μs | 0.049 μs |  1.26 |    0.05 |               2.9996 |                - |      - |   2.05 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 4           |  34.32 μs | 13.910 μs | 0.762 μs |  0.90 |    0.04 |               2.9998 |                - |      - |   2.05 KB |        1.00 |
|                               |             |           |           |          |       |         |                      |                  |        |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **8**           |  **36.89 μs** |  **2.219 μs** | **0.122 μs** |  **1.00** |    **0.00** |               **3.9517** |                **-** |      **-** |   **2.44 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 8           |  14.16 μs |  4.308 μs | 0.236 μs |  0.38 |    0.01 |               3.9357 |                - | 0.0153 |   2.47 KB |        1.01 |
| &#39;Concurrent LeastLatency&#39;     | 8           |  49.90 μs |  1.950 μs | 0.107 μs |  1.35 |    0.00 |               3.9451 |                - |      - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 8           |  49.80 μs |  4.255 μs | 0.233 μs |  1.35 |    0.01 |               3.9714 |                - |      - |   2.45 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 8           |  36.57 μs | 53.668 μs | 2.942 μs |  0.99 |    0.07 |               3.9645 |                - |      - |   2.45 KB |        1.00 |
|                               |             |           |           |          |       |         |                      |                  |        |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **16**          |  **39.25 μs** |  **0.983 μs** | **0.054 μs** |  **1.00** |    **0.00** |               **3.9945** |                **-** |      **-** |   **3.04 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 16          |  15.56 μs |  2.358 μs | 0.129 μs |  0.40 |    0.00 |               3.9735 |                - | 0.0305 |   3.07 KB |        1.01 |
| &#39;Concurrent LeastLatency&#39;     | 16          |  52.15 μs |  2.980 μs | 0.163 μs |  1.33 |    0.00 |               3.9710 |           0.0001 |      - |   3.04 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 16          |  51.53 μs |  8.406 μs | 0.461 μs |  1.31 |    0.01 |               3.9603 |           0.0001 |      - |   3.05 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 16          |  36.61 μs |  4.502 μs | 0.247 μs |  0.93 |    0.01 |               3.9597 |                - |      - |   3.06 KB |        1.00 |
